// -----------------------------------------------------------------------
// <copyright file="ChangeTrackerExtensions.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.EntityFramework;

using System;
using System.Linq;
using Kitpymes.Core.Entities.ShadowProperties;
using Kitpymes.Core.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

/*
   Clase de extensión ChangeTrackerExtensions
   Contiene las extensiones que detectan los cambios en la base de datos
*/

/// <summary>
/// Clase de extensión <c>ChangeTrackerExtensions</c>.
/// Contiene las extensiones que detectan los cambios en la base de datos.
/// </summary>
/// <remarks>
/// <para>En esta clase se pueden agregar todas las extensiones para detectar los cambios de estado de los objetos en la base de datos.</para>
/// </remarks>
public static class ChangeTrackerExtensions
{
    /// <summary>
    /// Detecta los cambios antes de guardar los datos al contexto.
    /// </summary>
    /// <typeparam name="TUserId">Tipo de dato del id de user.</typeparam>
    /// <param name="context">Contexto de la base de datos.</param>
    /// <param name="userId">Id del usuario.</param>
    /// <param name="enabled">Si se habilita.</param>
    /// <returns>DbContext | ArgumentException: 'context' o 'userId' es nulo o vacío.</returns>
    public static DbContext WithChangeTracker<TUserId>(this DbContext context, TUserId userId, bool enabled = true)
    {
        if (enabled)
        {
            context.ToThrowIfNullOrEmpty();

            if (context.ChangeTracker.HasChanges())
            {
                var entries = context.ChangeTracker.Entries().Where(entry => entry.Entity is not INotMapped);

                foreach (var entry in entries)
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            {
                                if (entry.Entity is IAuditedCreation)
                                {
                                    entry.Property(IAuditedCreation.CreatedDate).CurrentValue = DateTimeOffset.UtcNow;

                                    entry.Property(IAuditedCreation.CreatedUserId).CurrentValue = userId.ToThrowIfNullOrEmpty();
                                }

                                if (entry.Entity is IActive)
                                {
                                    entry.Property(IActive.IsActive).CurrentValue = true;
                                }

                                if (entry.Entity is IDelete)
                                {
                                    entry.Property(IDelete.IsDelete).CurrentValue = false;
                                }

                                if (entry.Entity is IRowVersion)
                                {
                                    entry.Property(IRowVersion.RowVersion).CurrentValue = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                                }

                                break;
                            }

                        case EntityState.Modified:

                            if (entry.Entity is IAuditedModification)
                            {
                                entry.Property(IAuditedModification.ModifiedDate).CurrentValue = DateTimeOffset.UtcNow;

                                entry.Property(IAuditedModification.ModifiedUserId).CurrentValue = userId.ToThrowIfNullOrEmpty();
                            }

                            if (entry.Entity is IRowVersion)
                            {
                                var rowVersion = entry.Property(IRowVersion.RowVersion);

                                if (rowVersion.OriginalValue != rowVersion.CurrentValue)
                                {
                                    var message = "El registro que intentó editar fue modificado por otro usuario después de obtener el valor original.";

                                    throw new DbUpdateConcurrencyException(message);
                                }

                                rowVersion.CurrentValue = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            }

                            break;

                        case EntityState.Deleted:
                            {
                                if (entry.Entity is IStatic)
                                {
                                    if ((bool?)entry.Property(IStatic.IsStatic).CurrentValue == true)
                                    {
                                        var message = $"No se puede eliminar la entidad porque es estática.";

                                        throw new DbUpdateException(message);
                                    }
                                }

                                entry.State = EntityState.Modified;

                                if (entry.Entity is IAuditedDeletion)
                                {
                                    entry.Property(IAuditedDeletion.DeletedDate).CurrentValue = DateTimeOffset.UtcNow;

                                    entry.Property(IAuditedDeletion.DeletedUserId).CurrentValue = userId.ToThrowIfNullOrEmpty();
                                }

                                if (entry.Entity is IDelete)
                                {
                                    entry.Property(IDelete.IsDelete).CurrentValue = true;
                                }

                                if (entry.Entity is IActive)
                                {
                                    entry.Property(IActive.IsActive).CurrentValue = false;
                                }

                                break;
                            }

                        default:
                            break;
                    }
                }
            }
        }

        return context;
    }

    /// <summary>
    /// Detecta los cambios antes de guardar los datos al contexto.
    /// </summary>
    /// <typeparam name="TTenantId">Tipo de dato del id de tenant.</typeparam>
    /// <param name="context">Contexto de la base de datos.</param>
    /// <param name="tenantId">Id del tenant.</param>
    /// <param name="enabled">Si se habilita.</param>
    /// <returns>DbContext.</returns>
    public static DbContext WithTenantChangeTracker<TTenantId>(this DbContext context, TTenantId tenantId, bool enabled = true)
    {
        if (enabled)
        {
            context.ToThrowIfNullOrEmpty();

            if (context.ChangeTracker.HasChanges())
            {
                var entries = context.ChangeTracker.Entries().Where(entry => entry.Entity is not INotMapped && entry.Entity is ITenant && entry.State is EntityState.Added);

                if (entries.Any())
                {
                    tenantId.ToThrowIfNullOrEmpty();

                    foreach (var entry in entries)
                    {
                        var oldTenantId = entry.Property(ITenant.TenantId).CurrentValue;

                        if (oldTenantId.ToIsNullOrEmpty() || oldTenantId.ToIsNotEqual(tenantId))
                        {
                            entry.Property(ITenant.TenantId).CurrentValue = tenantId;
                        }
                    }
                }
            }
        }

        return context;
    }
}
