using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kitpymes.Core.EntityFramework.Tests
{
    /// <summary>
    /// Al guardar (por el contexto o por la unidad de trabajo), el error de la base de datos sale tal cual lo lanza
    /// EF Core: la misma excepción, con su tipo, sus Entries y su InnerException. Antes se lanzaba una
    /// DbUpdateException nueva con el mensaje completo de la base de datos (que trae los valores, como un correo
    /// repetido) y sin la excepción original: el consumidor no podía traducir un duplicado a un conflicto y el valor
    /// acababa en el log. Y al fallar con transacción, el contexto ya no se libera.
    /// La BD en memoria no tiene transacciones de verdad: aquí no se prueba que se deshagan.
    /// </summary>
    [TestClass]
    public class EntityFrameworkDbContextSaveTests
    {
        public static IEnumerable<object[]> Guardados()
        {
            yield return new object[] { "SaveChanges" };
            yield return new object[] { "SaveChangesAsync" };
            yield return new object[] { "SaveChangesWithTransaction" };
            yield return new object[] { "SaveChangesWithTransactionAsync" };
            yield return new object[] { "UnitOfWork.Save" };
            yield return new object[] { "UnitOfWork.SaveAsync" };
            yield return new object[] { "UnitOfWork.Save con transacción" };
            yield return new object[] { "UnitOfWork.SaveAsync con transacción" };
        }

        public static IEnumerable<object[]> GuardadosConTransaccion()
        {
            yield return new object[] { "SaveChangesWithTransaction" };
            yield return new object[] { "SaveChangesWithTransactionAsync" };
            yield return new object[] { "UnitOfWork.Save con transacción" };
            yield return new object[] { "UnitOfWork.SaveAsync con transacción" };
        }

        [TestMethod]
        [DynamicData(nameof(Guardados))]
        public async Task Guardar_ConErrorDeLaBaseDeDatos_LanzaLaExcepcionOriginal(string guardado)
        {
            var original = new DbUpdateException(
                "An error occurred while saving the entity changes. See the inner exception for details.",
                new InvalidOperationException("Cannot insert duplicate key row. The duplicate key value is (ana@ejemplo.test)."));

            var (lanzada, context) = await GuardarConFallo(guardado, original).ConfigureAwait(false);
            await context.DisposeAsync().ConfigureAwait(false);

            Assert.AreSame(original, lanzada, $"{guardado} no lanza la excepción original sino {Describir(lanzada)}.");
        }

        [TestMethod]
        [DynamicData(nameof(Guardados))]
        public async Task Guardar_ConErrorDeConcurrencia_LanzaLaExcepcionOriginal(string guardado)
        {
            var original = new DbUpdateConcurrencyException("Concurrencia.");

            var (lanzada, context) = await GuardarConFallo(guardado, original).ConfigureAwait(false);
            await context.DisposeAsync().ConfigureAwait(false);

            Assert.AreSame(original, lanzada, $"{guardado} no lanza la excepción original sino {Describir(lanzada)}.");
        }

        [TestMethod]
        [DynamicData(nameof(Guardados))]
        public async Task Guardar_Cancelado_LanzaLaCancelacion(string guardado)
        {
            var original = new OperationCanceledException("Cancelado.");

            var (lanzada, context) = await GuardarConFallo(guardado, original).ConfigureAwait(false);
            await context.DisposeAsync().ConfigureAwait(false);

            Assert.AreSame(original, lanzada, $"{guardado} no lanza la cancelación sino {Describir(lanzada)}.");
        }

        [TestMethod]
        [DynamicData(nameof(GuardadosConTransaccion))]
        public async Task Guardar_ConTransaccionYError_NoLiberaElContexto(string guardado)
        {
            var (_, context) = await GuardarConFallo(guardado, new DbUpdateException("Fallo.")).ConfigureAwait(false);

            try
            {
                _ = await context.Set<FakeEntity>().CountAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                Assert.Fail($"{guardado}: tras el error el contexto está liberado.");
            }
            finally
            {
                await context.DisposeAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        [DynamicData(nameof(Guardados))]
        public async Task Guardar_SinError_GuardaLaEntidad(string guardado)
        {
            var nombre = $"bd-{Guid.NewGuid()}";

            using (var context = CrearContexto(nombre, interceptor: null))
            {
                context.Set<FakeEntity>().Add(NuevaEntidad());
                await Guardar(context, guardado).ConfigureAwait(false);
            }

            using (var context = CrearContexto(nombre, interceptor: null))
            {
                Assert.AreEqual(1, await context.Set<FakeEntity>().CountAsync().ConfigureAwait(false), $"{guardado} no ha guardado la entidad.");
            }
        }

        // Devuelve la excepción y el contexto (sin liberar: lo libera quien llama, si lo necesita).
        private static async Task<(Exception Lanzada, FakeContext Context)> GuardarConFallo(string guardado, Exception fallo)
        {
            var context = CrearContexto($"bd-{Guid.NewGuid()}", new FalloAlGuardarInterceptor(fallo));

            try
            {
                context.Set<FakeEntity>().Add(NuevaEntidad());
                await Guardar(context, guardado).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                return (exception, context);
            }

            await context.DisposeAsync().ConfigureAwait(false);
            throw new AssertFailedException($"{guardado} no ha lanzado ninguna excepción.");
        }

        private static FakeEntity NuevaEntidad() => new FakeEntity(1, "Uno", "Apellido", new FakeValueObject());

        private static Task Guardar(FakeContext context, string guardado)
        {
            var unitOfWork = new FakeUnitOfWork(context);

            return guardado switch
            {
                "SaveChanges" => Task.Run(() => context.SaveChanges()),
                "SaveChangesAsync" => context.SaveChangesAsync(),
                "SaveChangesWithTransaction" => Task.Run(() => context.SaveChangesWithTransaction()),
                "SaveChangesWithTransactionAsync" => context.SaveChangesWithTransactionAsync(),
                "UnitOfWork.Save" => Task.Run(() => unitOfWork.Save(useChangeTracker: false)),
                "UnitOfWork.SaveAsync" => unitOfWork.SaveAsync(useChangeTracker: false),
                "UnitOfWork.Save con transacción" => Task.Run(() =>
                {
                    unitOfWork.OpenTransaction();
                    unitOfWork.Save(useChangeTracker: false);
                }),
                "UnitOfWork.SaveAsync con transacción" => GuardarConTransaccionAsync(unitOfWork),
                _ => throw new ArgumentOutOfRangeException(nameof(guardado), guardado, null),
            };
        }

        private static async Task GuardarConTransaccionAsync(FakeUnitOfWork unitOfWork)
        {
            await unitOfWork.OpenTransactionAsync().ConfigureAwait(false);
            await unitOfWork.SaveAsync(useChangeTracker: false).ConfigureAwait(false);
        }

        private static FakeContext CrearContexto(string nombre, IInterceptor? interceptor)
        {
            var builder = new DbContextOptionsBuilder<FakeContext>()
                .UseInMemoryDatabase(nombre)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));

            if (interceptor is not null)
            {
                builder.AddInterceptors(interceptor);
            }

            return new FakeContext(builder.Options);
        }

        private static string Describir(Exception exception) => $"{exception.GetType().Name} ('{exception.Message}')";

        private sealed class FakeUnitOfWork : EntityFrameworkUnitOfWork<FakeContext>
        {
            public FakeUnitOfWork(FakeContext context)
                : base(context)
            {
            }
        }

        private sealed class FalloAlGuardarInterceptor : SaveChangesInterceptor
        {
            private readonly Exception fallo;

            public FalloAlGuardarInterceptor(Exception fallo) => this.fallo = fallo;

            public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
                => throw fallo;

            public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
                => throw fallo;
        }
    }
}
