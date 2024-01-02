// -----------------------------------------------------------------------
// <copyright file="LoggerExtensions.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.EntityFramework;

using System;
using Kitpymes.Core.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/*
   Clase de extensión LoggerExtensions
   Contiene las extensiones para aplicar el logeo de errores
*/

/// <summary>
/// Clase de extensión <c>LoggerExtensions</c>.
/// Contiene las extensiones para aplicar el logeo de errores.
/// </summary>
/// <remarks>
/// <para>En esta clase se pueden agregar todas las extensiones para aplicar el logeo de errores.</para>
/// </remarks>
public static class LoggerExtensions
{
    /// <summary>
    /// Logea en la consola los cambios que se producen en la DB.
    /// </summary>
    /// <param name="context">Contexto.</param>
    /// <param name="enabled">Si esta habilitado.</param>
    /// <returns>DbContext.</returns>
    public static DbContext WithLogSaveChanges(this DbContext context, bool enabled = true)
    {
        if (enabled)
        {
            if (context.ToThrowIfNullOrEmpty().ChangeTracker.HasChanges())
            {
                Console.WriteLine(context.ChangeTracker.DebugView.LongView);
            }
        }

        return context;
    }

    internal static DbContextOptionsBuilder WithLogError(this DbContextOptionsBuilder optionsBuilder, bool enabled = true)
    {
        if (enabled)
        {
            optionsBuilder

                // .ConfigureWarnings(x => x

                // // Convertir una advertencia en un error
                //    .Throw(RelationalEventId.MultipleCollectionIncludeWarning)

                // // Para que logee las ejecuciones de comando en nivel debug
                //    .Log(
                //        (RelationalEventId.CommandExecuting, LogLevel.Debug)))
                .LogTo(
                    action: Console.WriteLine, // categories: new[] { DbLoggerCategory.ChangeTracking.Name, DbLoggerCategory.Database.Command.Name },
                    minimumLevel: LogLevel.Error,
                    options: DbContextLoggerOptions.DefaultWithUtcTime)
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors();
        }

        return optionsBuilder;
    }
}
