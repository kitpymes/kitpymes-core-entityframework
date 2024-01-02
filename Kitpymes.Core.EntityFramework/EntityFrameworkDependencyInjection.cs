// -----------------------------------------------------------------------
// <copyright file="EntityFrameworkDependencyInjection.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.EntityFramework;

using System;
using Kitpymes.Core.Shared.Extensions;
using Kitpymes.Core.Shared.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using static System.Formats.Asn1.AsnWriter;

/*
    Clase de extensión EntityFrameworkDependencyInjection
    Contiene las extensiones de los servicios de entity framework
*/

/// <summary>
/// Clase de extensión <c>EntityFrameworkDependencyInjection</c>.
/// Contiene las extensiones de los servicios de entity framework.
/// </summary>
/// <remarks>
/// <para>En esta clase se pueden agregar todas las extensiones de los servicios para entity framework.</para>
/// </remarks>
public static class EntityFrameworkDependencyInjection
{
    /// <summary>
    /// Carga la configuración de la base de datos.
    /// </summary>
    /// <typeparam name="TDbContext">Tipo de contexto.</typeparam>
    /// <param name="services">Collección de servicios.</param>
    /// <returns>TDbContext | ApplicationException.</returns>
    public static IServiceCollection LoadDatabase<TDbContext>(this IServiceCollection services)
        where TDbContext : EntityFrameworkDbContext
    {
        var databaseSettings = services.ToSettings<DatabaseSettings>().ToThrowIfNullOrEmpty();

        var dbProvider = databaseSettings.DbProvider.ToEnum<DbProvider>();

        switch (dbProvider)
        {
            case DbProvider.Memory:
                services.LoadInMemoryDatabase<TDbContext>();
                break;
            case DbProvider.SqlServer:
                services.LoadSqlServer<TDbContext>(databaseSettings.SqlServerSettings);
                break;
            case DbProvider.SQLite:
            default:
                Verify.ToThrowIf($"Unsupported database provider: {databaseSettings.DbProvider}", () => true);
                break;
        }

        return services;
    }

    /// <summary>
    /// Carga la configuración de la base de datos.
    /// </summary>
    /// <typeparam name="TIDbContext">Tipo de interfaz del contexto.</typeparam>
    /// <typeparam name="TDbContext">Tipo de contexto.</typeparam>
    /// <param name="services">Collección de servicios.</param>
    /// <returns>TDbContext | ApplicationException.</returns>
    public static IServiceCollection LoadDatabase<TIDbContext, TDbContext>(
        this IServiceCollection services)
      where TIDbContext : class
      where TDbContext : EntityFrameworkDbContext, TIDbContext
    {
        services.AddScoped<TIDbContext, TDbContext>();

        return services.LoadDatabase<TDbContext>();
    }

    #region SqlServer

    /// <summary>
    /// Carga un contexto de datos.
    /// </summary>
    /// <typeparam name="TDbContext">Tipo de contexto.</typeparam>
    /// <param name="services">Collección de servicios.</param>
    /// <param name="sqlServerSettings">Configuración de sql server y entity framework.</param>
    /// <returns>TDbContext | ApplicationException.</returns>
    public static TDbContext LoadSqlServer<TDbContext>(
       this IServiceCollection services,
       SqlServerSettings? sqlServerSettings)
    where TDbContext : EntityFrameworkDbContext
    {
        sqlServerSettings.ToThrowIfNullOrEmpty();

        var entityFrameworkSettings = sqlServerSettings as EntityFrameworkSettings;

        entityFrameworkSettings.DbContextOptions = dbContextOptions => dbContextOptions
            .UseSqlServer(sqlServerSettings.Connection, sqlServerSettings.SqlServerOptions)
            .WithLogError(sqlServerSettings.LogErrors == true);

        var context = services.LoadContext<TDbContext>(entityFrameworkSettings);

        return context;
    }

    /// <summary>
    /// Carga un contexto de datos.
    /// </summary>
    /// <typeparam name="TDbContext">Tipo de contexto.</typeparam>
    /// <param name="services">Collección de servicios.</param>
    /// <param name="sqlServerOptions">Configuración de sql server y entity framework.</param>
    /// <returns>TDbContext | ApplicationException.</returns>
    public static TDbContext LoadSqlServer<TDbContext>(
        this IServiceCollection services,
        Action<SqlServerOptions> sqlServerOptions)
            where TDbContext : EntityFrameworkDbContext
    {
        var settings = sqlServerOptions.ToConfigureOrDefault().SqlServerSettings;

        return services.LoadSqlServer<TDbContext>(settings);
    }

    #endregion SqlServer

    #region SqlServerWithUnitOfWork

    /// <summary>
    /// Carga un contexto de datos.
    /// </summary>
    /// <typeparam name="TDbContext">Tipo de contexto.</typeparam>
    /// <typeparam name="TUnitOfWork">Tipo de unidad de trabajo.</typeparam>
    /// <param name="services">Collección de servicios.</param>
    /// <param name="sqlServerOptions">Configuración de sql server y entity framework.</param>
    /// <returns>TDbContext | ApplicationException.</returns>
    public static TDbContext LoadSqlServer<TDbContext, TUnitOfWork>(
        this IServiceCollection services,
        Action<SqlServerOptions> sqlServerOptions)
            where TDbContext : EntityFrameworkDbContext
            where TUnitOfWork : EntityFrameworkUnitOfWork<TDbContext>
    {
        var settings = sqlServerOptions.ToConfigureOrDefault().SqlServerSettings;

        return services.LoadSqlServer<TDbContext, TUnitOfWork>(settings);
    }

    /// <summary>
    /// Carga un contexto de datos.
    /// </summary>
    /// <typeparam name="TDbContext">Tipo de contexto.</typeparam>
    /// <typeparam name="TUnitOfWork">Tipo de unidad de trabajo.</typeparam>
    /// <param name="services">Collección de servicios.</param>
    /// <param name="sqlServerSettings">Configuración de sql server y entity framework.</param>
    /// <returns>TDbContext | ApplicationException.</returns>
    public static TDbContext LoadSqlServer<TDbContext, TUnitOfWork>(
        this IServiceCollection services,
        SqlServerSettings sqlServerSettings)
            where TDbContext : EntityFrameworkDbContext
            where TUnitOfWork : EntityFrameworkUnitOfWork<TDbContext>
    {
        var settings = sqlServerSettings.ToThrowIfNullOrEmpty();

        services
            .AddScoped<EntityFrameworkDbContext, TDbContext>()
            .AddScoped<IEntityFrameworkUnitOfWork, TUnitOfWork>();

        settings.DbContextOptions = dbContextOptions => dbContextOptions
                .UseSqlServer(settings.Connection, settings.SqlServerOptions)
                .WithLogError(settings.LogErrors == true);

        return services.LoadContext<TDbContext>(settings);
    }

    #endregion SqlServerWithUnitOfWork

    #region Inmemory

    /// <summary>
    /// Carga un contexto de datos.
    /// </summary>
    /// <typeparam name="TDbContext">Tipo de contexto.</typeparam>
    /// <param name="services">Collección de servicios.</param>
    /// <param name="inMemoryDbContextOptions">Configuración del contexto.</param>
    /// <param name="databaseName">Nombre de la base de datos.</param>
    /// <returns>TDbContext | ApplicationException.</returns>
    public static TDbContext LoadInMemoryDatabase<TDbContext>(
        this IServiceCollection services,
        Action<InMemoryDbContextOptionsBuilder>? inMemoryDbContextOptions = null,
        string? databaseName = null)
            where TDbContext : DbContext
    {
        databaseName ??= typeof(TDbContext).Name;

        void ContextOptions(EntityFrameworkOptions dbContextOptions) => dbContextOptions
            .WithOptions(options => options
                .UseInMemoryDatabase(databaseName, inMemoryDbContextOptions)
                .WithLogError());

        return services.LoadContext<TDbContext>(ContextOptions);
    }

    #endregion Inmemory

    #region DbContext

    /// <summary>
    /// Carga un contexto de datos.
    /// </summary>
    /// <typeparam name="TDbContext">Tipo de contexto.</typeparam>
    /// <param name="services">Collección de servicios.</param>
    /// <param name="entityFrameworkOptions">Configuración de entity framework.</param>
    /// <returns>TDbContext | ApplicationException.</returns>
    public static TDbContext LoadContext<TDbContext>(
        this IServiceCollection services,
        Action<EntityFrameworkOptions> entityFrameworkOptions)
            where TDbContext : DbContext
    {
        var settings = entityFrameworkOptions.ToConfigureOrDefault().EntityFrameworkSettings;

        return services.LoadContext<TDbContext>(settings);
    }

    /// <summary>
    /// Carga un contexto de datos.
    /// </summary>
    /// <typeparam name="TDbContext">Tipo de contexto.</typeparam>
    /// <param name="services">Collección de servicios.</param>
    /// <param name="entityFrameworkSettings">Configuración de entity framework.</param>
    /// <returns>TDbContext | ApplicationException.</returns>
    public static TDbContext LoadContext<TDbContext>(
       this IServiceCollection services,
       EntityFrameworkSettings entityFrameworkSettings)
           where TDbContext : DbContext
    {
        var settings = entityFrameworkSettings.ToThrowIfNullOrEmpty();

        var context = services
            .AddDbContextFactory<TDbContext>(settings.DbContextOptions, ServiceLifetime.Scoped)
                .ToService<TDbContext>()
                    .ToThrowIfNullOrEmpty();

        if (settings.Delete == true)
        {
            context.Database.EnsureDeleted();
        }

        if (settings.Create == true)
        {
            context.Database.EnsureCreated();
        }
        else if (settings.Migrate == true)
        {
            context.Database.Migrate();
        }

        return context;
    }

    #endregion DbContext
}