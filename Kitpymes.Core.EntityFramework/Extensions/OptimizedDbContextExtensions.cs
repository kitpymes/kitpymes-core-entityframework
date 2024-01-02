// -----------------------------------------------------------------------
// <copyright file="OptimizedDbContextExtensions.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.EntityFramework;

using Kitpymes.Core.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

/*
   Clase de extensión OptimizedDbContextExtensions
   Contiene las extensiones para optimizar la carga de datos del contexto
*/

/// <summary>
/// Clase de extensión <c>OptimizedDbContextExtensions</c>.
/// Contiene las extensiones para optimizar la carga de datos del contexto.
/// </summary>
/// <remarks>
/// <para>En esta clase se pueden agregar todas las extensiones para optimizar la carga de datos del contexto.</para>
/// </remarks>
public static class OptimizedDbContextExtensions
{
    /// <summary>
    /// Configuración para optimizar el contexto.
    /// <list type="bullet">
    /// <item>
    ///     <term>LazyLoadingEnabled = false</term>
    ///     <description>Desabilita la carga de las propiedades de navegación de las entidades del contexto.</description>
    /// </item>
    /// <item>
    ///     <term>QueryTrackingBehavior = NoTracking</term>
    ///     <description>Desabilita el rastreador de cambios de las entidades del contexto.</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <param name="context">Contexto de datos.</param>
    /// <param name="enabled">Si se habilita la configuración.</param>
    /// <returns>DbContext.</returns>
    public static DbContext WithOptimized(this DbContext context, bool enabled = true)
    {
        if (enabled)
        {
            context.ToThrowIfNullOrEmpty();

            context.ChangeTracker.LazyLoadingEnabled = false;

            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        return context;
    }
}
