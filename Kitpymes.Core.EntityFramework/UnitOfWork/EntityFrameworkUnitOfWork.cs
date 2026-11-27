// -----------------------------------------------------------------------
// <copyright file="EntityFrameworkUnitOfWork.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.EntityFramework;

using System;
using System.Data;
using System.Threading.Tasks;
using Kitpymes.Core.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

/// <inheritdoc/>
public abstract class EntityFrameworkUnitOfWork<TDbContext> : IEntityFrameworkUnitOfWork
    where TDbContext : DbContext
{
    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="EntityFrameworkUnitOfWork{TDbContext}"/>.
    /// </summary>
    /// <param name="context">Contexto de datos.</param>
    protected EntityFrameworkUnitOfWork(TDbContext context)
    => Context = context.ToThrowIfNullOrEmpty(nameof(context));

    /// <inheritdoc/>
    public IDbContextTransaction Transaction { get; private set; } = null!;

    private TDbContext Context { get; }

    #region Transaction

    /// <inheritdoc/>
    public virtual void OpenTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        Transaction?.Dispose();

        Transaction = Context.Database.BeginTransaction(isolationLevel);
    }

    /// <inheritdoc/>
    public virtual async Task OpenTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        if (Transaction is not null)
        {
            await Transaction.DisposeAsync().ConfigureAwait(false);
        }

        Transaction = await Context.Database.BeginTransactionAsync(isolationLevel).ConfigureAwait(false);
    }

    #endregion Transaction

    #region Save

    /// <inheritdoc/>
    public virtual void Save(bool useChangeTracker = true)
    {
        try
        {
            Context.WithChangeTracker(useChangeTracker).SaveChanges();

            Transaction?.Commit();
        }
        catch
        {
            CloseTransaction();

            throw;
        }
    }

    /// <inheritdoc/>
    public virtual async Task SaveAsync(bool useChangeTracker = true)
    {
        try
        {
            await Context.WithChangeTracker(useChangeTracker).SaveChangesAsync().ConfigureAwait(false);

            if (Transaction is not null)
            {
                await Transaction.CommitAsync().ConfigureAwait(false);
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
            if (Context.Database.IsRelational())
            {
                Context.Database.CloseConnection();
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
            if (Context.Database.IsRelational())
            {
                await Context.Database.CloseConnectionAsync().ConfigureAwait(false);
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