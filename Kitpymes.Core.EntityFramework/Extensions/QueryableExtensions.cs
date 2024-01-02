// -----------------------------------------------------------------------
// <copyright file="QueryableExtensions.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.EntityFramework;

using System;
using System.Linq;
using System.Linq.Expressions;
using Kitpymes.Core.Entities.ShadowProperties;
using Kitpymes.Core.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

/*
    Clase de extensión QueryableExtensions
    Contiene las extensiones para consultar la base de datos
*/

/// <summary>
/// Clase de extensión <c>QueryableExtensions</c>.
/// Contiene las extensiones para consultar la base de datos.
/// </summary>
/// <remarks>
/// <para>En esta clase se pueden agregar todas las extensiones para consultar la base de datos.</para>
/// </remarks>
public static class QueryableExtensions
{
    /// <summary>
    /// Hace que un contexto sea mas rápido para obtener información de la base de datos.
    /// </summary>
    /// <typeparam name="T">Tipo de entidad a consultar.</typeparam>
    /// <param name="context">Contexto de datos.</param>
    /// <returns>IQueryable{T} | ArgumentException: context es nulo.</returns>
    public static IQueryable<T> ToQueryable<T>(this DbContext context)
        where T : class
    => context.ToThrowIfNullOrEmpty().Set<T>();

    /// <summary>
    /// Incluye entidades asociadas a la consulta.
    /// </summary>
    /// <typeparam name="T">Tipo de entidad a consultar.</typeparam>
    /// <param name="queryable">Consulta del contexto.</param>
    /// <param name="includes">Las entidades a incluir.</param>
    /// <returns>IQueryable{T}.</returns>
    public static IQueryable<T> ToInclude<T>(this IQueryable<T> queryable, Expression<Func<T, object>>[] includes)
        where T : class
    {
        includes?.ToList().ForEach(include => queryable = queryable.Include(include));

        return queryable;
    }

    /// <summary>
    /// Filtra una secuencia de valores basada en un predicado.
    /// </summary>
    /// <typeparam name="TEntity">Entidad.</typeparam>
    /// <param name="queryable">Instancia IQueryable.</param>
    /// <param name="tenantId">Id del tenant.</param>
    /// <returns>La lista con el filtro.</returns>
    public static IQueryable<TEntity> ToTenantFilter<TEntity>(this IQueryable<TEntity> queryable, Guid tenantId)
        where TEntity : class
    {
        queryable = queryable.ToWhere(x => EF.Property<Guid>(x, ITenant.TenantId) == tenantId);

        return queryable;
    }

    /// <summary>
    /// Filtra una secuencia de valores basada en un predicado.
    /// </summary>
    /// <typeparam name="TEntity">Entidad.</typeparam>
    /// <param name="queryable">Instancia IQueryable.</param>
    /// <param name="idDefault">Si es default.</param>
    /// <returns>La lista con el filtro.</returns>
    public static IQueryable<TEntity> ToDefaultFilter<TEntity>(this IQueryable<TEntity> queryable, bool idDefault = true)
       where TEntity : class
    {
        queryable = queryable.ToWhere(x => EF.Property<bool>(x, IDefault.IsDefault) == idDefault);

        return queryable;
    }

    /// <summary>
    /// Filtra una secuencia de valores basada en un predicado.
    /// </summary>
    /// <typeparam name="TEntity">Entidad.</typeparam>
    /// <param name="queryable">Instancia IQueryable.</param>
    /// <param name="idActive">Si es activo.</param>
    /// <returns>La lista con el filtro.</returns>
    public static IQueryable<TEntity> ToActiveFilter<TEntity>(this IQueryable<TEntity> queryable, bool idActive = true)
       where TEntity : class
    {
        queryable = queryable.ToWhere(x => EF.Property<bool>(x, IActive.IsActive) == idActive);

        return queryable;
    }

    /// <summary>
    /// Filtra una secuencia de valores basada en un predicado.
    /// </summary>
    /// <typeparam name="TEntity">Entidad.</typeparam>
    /// <param name="queryable">Instancia IQueryable.</param>
    /// <param name="idStatic">Si es estático.</param>
    /// <returns>La lista con el filtro.</returns>
    public static IQueryable<TEntity> ToStaticFilter<TEntity>(this IQueryable<TEntity> queryable, bool idStatic = true)
       where TEntity : class
    {
        queryable = queryable.ToWhere(x => EF.Property<bool>(x, IStatic.IsStatic) == idStatic);

        return queryable;
    }

    /// <summary>
    /// Filtra una secuencia de valores basada en un predicado.
    /// </summary>
    /// <typeparam name="TEntity">Entidad.</typeparam>
    /// <param name="queryable">Instancia IQueryable.</param>
    /// <param name="isDelete">Si esta eliminado.</param>
    /// <returns>La lista con el filtro.</returns>
    public static IQueryable<TEntity> ToDeleteFilter<TEntity>(this IQueryable<TEntity> queryable, bool isDelete = true)
       where TEntity : class
    {
        queryable = queryable.ToWhere(x => EF.Property<bool>(x, IDelete.IsDelete) == isDelete);

        return queryable;
    }

    /// <summary>
    /// Filtra una secuencia de valores basada en un predicado.
    /// </summary>
    /// <typeparam name="TEntity">Entidad.</typeparam>
    /// <param name="queryable">Instancia IQueryable.</param>
    /// <param name="isNotVisible">Si es no visible.</param>
    /// <returns>La lista con el filtro.</returns>
    public static IQueryable<TEntity> ToNotVisibleFilter<TEntity>(this IQueryable<TEntity> queryable, bool isNotVisible = true)
       where TEntity : class
    {
        queryable = queryable.ToWhere(x => EF.Property<bool>(x, INotVisible.IsNotVisible) == isNotVisible);

        return queryable;
    }
}
