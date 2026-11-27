// -----------------------------------------------------------------------
// <copyright file="EntityFrameworkDbContext.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.EntityFramework;

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

/*
   Clase de extensión EntityFrameworkDbContext
   Contiene el contexto base para entity framework
*/

/// <summary>
/// Clase de extensión <c>EntityFrameworkDbContext</c>.
/// Contiene el contexto base para entity framework.
/// </summary>
/// <remarks>
/// <para>En esta clase se pueden agregar todas las acciones comunes para el contexto de entity framework.</para>
/// </remarks>
public abstract class EntityFrameworkDbContext : DbContext, IEntityFrameworkDbContext
{
    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="EntityFrameworkDbContext"/>.
    /// </summary>
    /// <param name="options">Configuración del contexto.</param>
    protected EntityFrameworkDbContext(DbContextOptions options)
        : base(options)
    {
    }

    private IDbContextTransaction Transaction { get; set; } = null!;

    #region Save

    /// <inheritdoc/>
    /// <remarks>
    /// <para>Si falla, lanza la excepción que lanza EF Core, sin cambiarla (con su tipo, sus <c>Entries</c> y su <c>InnerException</c>).</para>
    /// </remarks>
    public new virtual void SaveChanges() => base.SaveChanges();

    /// <inheritdoc/>
    /// <remarks>
    /// <para>Si falla, lanza la excepción que lanza EF Core, sin cambiarla (con su tipo, sus <c>Entries</c> y su <c>InnerException</c>).</para>
    /// </remarks>
    public new virtual async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    /// <remarks>
    /// <para>Si falla, deshace la transacción, cierra la conexión y lanza la excepción que lanza EF Core, sin cambiarla. El contexto no se libera.</para>
    /// </remarks>
    public virtual void SaveChangesWithTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        try
        {
            Transaction?.Dispose();

            Transaction = Database.BeginTransaction(isolationLevel);

            base.SaveChanges();

            Transaction?.Commit();
        }
        catch
        {
            CloseTransaction();

            throw;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>Si falla, deshace la transacción, cierra la conexión y lanza la excepción que lanza EF Core, sin cambiarla. El contexto no se libera.</para>
    /// </remarks>
    public virtual async Task SaveChangesWithTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
    {
        try
        {
            if (Transaction is not null)
            {
                await Transaction.DisposeAsync().ConfigureAwait(false);
            }

            Transaction = await Database.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);

            await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            if (Transaction is not null)
            {
                await Transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            await CloseTransactionAsync().ConfigureAwait(false);

            throw;
        }
    }

    #endregion Save

    #region Private

    // Deshace la transacción y cierra la conexión. Un error al cerrar no puede tapar el error original, que es el
    // que se relanza, y el contexto no se libera: es de quien lo creó (normalmente, el contenedor de dependencias).
    private void CloseTransaction()
    {
        try
        {
            Transaction?.Dispose();

            // Cerrar la conexión sólo existe en los proveedores relacionales.
            if (Database.IsRelational())
            {
                Database.CloseConnection();
            }
        }
        catch (Exception)
        {
            // Se ignora a propósito: sale el error original.
        }
        finally
        {
            Transaction = null!;
        }
    }

    private async Task CloseTransactionAsync()
    {
        try
        {
            if (Transaction is not null)
            {
                await Transaction.DisposeAsync().ConfigureAwait(false);
            }

            // Cerrar la conexión sólo existe en los proveedores relacionales.
            if (Database.IsRelational())
            {
                await Database.CloseConnectionAsync().ConfigureAwait(false);
            }
        }
        catch (Exception)
        {
            // Se ignora a propósito: sale el error original.
        }
        finally
        {
            Transaction = null!;
        }
    }

    #endregion Private
}
