using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kitpymes.Core.EntityFramework.Tests
{
    /// <summary>
    /// Al guardar, el error de la base de datos sale tal cual lo lanza EF Core: la misma excepción, con su tipo,
    /// sus Entries y su InnerException. Antes se lanzaba una DbUpdateException nueva con el mensaje completo de
    /// la base de datos (que trae los valores, como un correo repetido) y sin la excepción original, así que el
    /// consumidor no podía traducir un índice único repetido a un conflicto y el valor acababa en el log.
    /// </summary>
    [TestClass]
    public class EntityFrameworkDbContextSaveTests
    {
        private const string ValorDeLaBaseDeDatos = "ana@ejemplo.test";

        public static System.Collections.Generic.IEnumerable<object[]> Guardados()
        {
            yield return new object[] { "SaveChanges" };
            yield return new object[] { "SaveChangesAsync" };
            yield return new object[] { "SaveChangesWithTransaction" };
            yield return new object[] { "SaveChangesWithTransactionAsync" };
        }

        [TestMethod]
        [DynamicData(nameof(Guardados))]
        public async Task Guardar_ConErrorDeLaBaseDeDatos_LanzaLaExcepcionOriginal(string guardado)
        {
            var original = new DbUpdateException(
                "An error occurred while saving the entity changes. See the inner exception for details.",
                new InvalidOperationException($"Cannot insert duplicate key row. The duplicate key value is ({ValorDeLaBaseDeDatos})."));

            var lanzada = await GuardarConFallo(guardado, original).ConfigureAwait(false);

            Assert.AreSame(original, lanzada, $"{guardado} no lanza la excepción original sino {Describir(lanzada)}.");
            Assert.IsFalse(lanzada.Message.Contains(ValorDeLaBaseDeDatos, StringComparison.Ordinal), $"{guardado}: el mensaje trae el valor de la base de datos.");
        }

        [TestMethod]
        [DynamicData(nameof(Guardados))]
        public async Task Guardar_ConErrorDeConcurrencia_LanzaLaExcepcionOriginal(string guardado)
        {
            var original = new DbUpdateConcurrencyException("Concurrencia.");

            var lanzada = await GuardarConFallo(guardado, original).ConfigureAwait(false);

            Assert.AreSame(original, lanzada, $"{guardado} no lanza la excepción original sino {Describir(lanzada)}.");
        }

        [TestMethod]
        [DynamicData(nameof(Guardados))]
        public async Task Guardar_Cancelado_LanzaLaCancelacion(string guardado)
        {
            var original = new OperationCanceledException("Cancelado.");

            var lanzada = await GuardarConFallo(guardado, original).ConfigureAwait(false);

            Assert.AreSame(original, lanzada, $"{guardado} no lanza la cancelación sino {Describir(lanzada)}.");
        }

        [TestMethod]
        [DynamicData(nameof(Guardados))]
        public async Task Guardar_SinError_GuardaLaEntidad(string guardado)
        {
            var nombre = $"bd-{Guid.NewGuid()}";

            using (var context = CrearContexto(nombre, interceptor: null))
            {
                context.Set<FakeEntity>().Add(new FakeEntity(1, "Uno"));
                await Guardar(context, guardado).ConfigureAwait(false);
            }

            using (var context = CrearContexto(nombre, interceptor: null))
            {
                Assert.AreEqual(1, await context.Set<FakeEntity>().CountAsync().ConfigureAwait(false), $"{guardado} no ha guardado la entidad.");
            }
        }

        private static async Task<Exception> GuardarConFallo(string guardado, Exception fallo)
        {
            var context = CrearContexto($"bd-{Guid.NewGuid()}", new FalloAlGuardarInterceptor(fallo));

            try
            {
                context.Set<FakeEntity>().Add(new FakeEntity(1, "Uno"));
                await Guardar(context, guardado).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                return exception;
            }
            finally
            {
                await context.DisposeAsync().ConfigureAwait(false);
            }

            Assert.Fail($"{guardado} no ha lanzado ninguna excepción.");
            return null!;
        }

        private static Task Guardar(FakeContext context, string guardado) => guardado switch
        {
            "SaveChanges" => Task.Run(() => context.SaveChanges()),
            "SaveChangesAsync" => context.SaveChangesAsync(),
            "SaveChangesWithTransaction" => Task.Run(() => context.SaveChangesWithTransaction()),
            "SaveChangesWithTransactionAsync" => context.SaveChangesWithTransactionAsync(),
            _ => throw new ArgumentOutOfRangeException(nameof(guardado), guardado, null),
        };

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
