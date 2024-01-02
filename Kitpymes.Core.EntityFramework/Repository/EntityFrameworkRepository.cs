// -----------------------------------------------------------------------
// <copyright file="EntityFrameworkRepository.cs" company="Kitpymes">
// Copyright (c) Kitpymes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project docs folder for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Kitpymes.Core.EntityFramework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Kitpymes.Core.Entities;
using Kitpymes.Core.Repositories;
using Kitpymes.Core.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

/// <inheritdoc/>
/// <summary>
/// Inicializa una nueva instancia de la clase <see cref="EntityFrameworkRepository{T}"/>.
/// </summary>
/// <param name="context">Contexto de datos.</param>
public class EntityFrameworkRepository<T>(DbContext context)
    : IRelationalRepository<T>
    where T : class, IEntityBase
{
    private DbContext Context { get; } = context;

    #region Get

    /// <inheritdoc/>
    public T? GetByKey(params object[] keyValues)
    => Context.Find<T>(keyValues);

    /// <inheritdoc/>
    public async Task<T?> GetByKeyAsync(params object[] keyValues)
    => await Context.FindAsync<T>(keyValues).ConfigureAwait(false);

    /// <inheritdoc/>
    public T? Get(Expression<Func<T, bool>> where)
    => Context.ToQueryable<T>().FirstOrDefault(where);

    /// <inheritdoc/>
    public async Task<T?> GetAsync(Expression<Func<T, bool>> where)
    => await Context.ToQueryable<T>().FirstOrDefaultAsync(where).ConfigureAwait(false);

    /// <inheritdoc/>
    public T? Get(Expression<Func<T, bool>> where, params Expression<Func<T, object>>[] includes)
    => Context.ToQueryable<T>().ToInclude(includes).FirstOrDefault(where);

    /// <inheritdoc/>
    public async Task<T?> GetAsync(Expression<Func<T, bool>> where, params Expression<Func<T, object>>[] includes)
    => await Context.ToQueryable<T>().ToInclude(includes).FirstOrDefaultAsync(where).ConfigureAwait(false);

    /// <inheritdoc/>
    public TResult? Get<TResult>(
        Expression<Func<T, TResult>> select,
        Expression<Func<T, bool>>? where = null,
        params Expression<Func<T, object>>[] includes)
    => Context.ToQueryable<T>().ToInclude(includes).ToWhere(where).Select(select).First();

    /// <inheritdoc/>
    public async Task<TResult?> GetAsync<TResult>(
        Expression<Func<T, TResult>> select,
        Expression<Func<T, bool>>? where = null,
        params Expression<Func<T, object>>[] includes)
    => await Context.ToQueryable<T>().ToInclude(includes).ToWhere(where).Select(select).FirstOrDefaultAsync().ConfigureAwait(false);

    #endregion Get

    #region GetAll

    /// <summary>
    /// Obtiene todos los elementos de una tabla.
    /// </summary>
    /// <returns>IEnumerable{T}.</returns>
    public IEnumerable<T> GetAll()
    => Context.ToQueryable<T>().ToList();

    /// <summary>
    /// Obtiene todos los elementos de una tabla.
    /// </summary>
    /// <returns>IEnumerable{T}.</returns>
    public async Task<IEnumerable<T>> GetAllAsync()
    => await Context.ToQueryable<T>().ToListAsync().ConfigureAwait(false);

    /// <inheritdoc/>
    public IEnumerable<T> GetAll(Expression<Func<T, bool>> where)
    => Context.ToQueryable<T>().ToWhere(where).ToList();

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> where)
    => await Context.ToQueryable<T>().ToWhere(where).ToListAsync().ConfigureAwait(false);

    /// <inheritdoc/>
    public IEnumerable<T> GetAll(
        Expression<Func<T, bool>> where,
        params Expression<Func<T, object>>[] includes)
    => Context.ToQueryable<T>().ToWhere(where).ToInclude(includes);

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> GetAllAsync(
        Expression<Func<T, bool>> where,
        params Expression<Func<T, object>>[] includes)
    => await Context.ToQueryable<T>().ToWhere(where).ToInclude(includes).ToListAsync().ConfigureAwait(false);

    /// <inheritdoc/>
    public IEnumerable<TResult> GetAll<TResult>(
        Expression<Func<T, TResult>> select,
        Expression<Func<T, bool>>? where = null,
        params Expression<Func<T, object>>[] includes)
    => Context.ToQueryable<T>().ToWhere(where).ToInclude(includes).Select(select);

    /// <inheritdoc/>
    public async Task<IEnumerable<TResult>> GetAllAsync<TResult>(
        Expression<Func<T, TResult>> select,
        Expression<Func<T, bool>>? where = null,
        params Expression<Func<T, object>>[] includes)
    => await Context.ToQueryable<T>().ToWhere(where).ToInclude(includes).Select(select).ToListAsync().ConfigureAwait(false);

    /// <inheritdoc/>
    public IEnumerable<T> GetAllPaged(Action<PagedOptions> options)
    {
        var settings = options.ToConfigureOrDefault();

        return Context.ToQueryable<T>()
            .ToPaged(settings.Properties, settings.Ascending, settings.Index, settings.Size);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> GetAllPagedAsync(Action<PagedOptions> options)
    => await Task.FromResult(GetAllPaged(options)).ConfigureAwait(false);

    /// <inheritdoc/>
    public IEnumerable<T> GetAllPaged(
        Action<PagedOptions>? options = null,
        Expression<Func<T, bool>>? where = null,
        params Expression<Func<T, object>>[] includes)
    {
        var settings = options.ToConfigureOrDefault();

        return Context.ToQueryable<T>().ToWhere(where).ToInclude(includes)
            .ToPaged(settings.Properties, settings.Ascending, settings.Index, settings.Size);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> GetAllPagedAsync(
        Action<PagedOptions>? options = null,
        Expression<Func<T, bool>>? where = null,
        params Expression<Func<T, object>>[] includes)
    {
        var settings = options.ToConfigureOrDefault();

        return await Context.ToQueryable<T>().ToWhere(where).ToInclude(includes)
            .ToPaged(settings.Properties, settings.Ascending, settings.Index, settings.Size).ToListAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public IEnumerable<TResult> GetAllPaged<TResult>(
        Expression<Func<T, TResult>> select,
        Action<PagedOptions>? options = null,
        Expression<Func<T, bool>>? where = null,
        params Expression<Func<T, object>>[] includes)
    {
        var settings = options.ToConfigureOrDefault();

        return Context.ToQueryable<T>().ToWhere(where).ToInclude(includes).Select(select)
            .ToPaged(settings.Properties, settings.Ascending, settings.Index, settings.Size);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TResult>> GetAllPagedAsync<TResult>(
        Expression<Func<T, TResult>> select,
        Action<PagedOptions>? options = null,
        Expression<Func<T, bool>>? where = null,
        params Expression<Func<T, object>>[] includes)
    {
        var settings = options.ToConfigureOrDefault();

        return await Context.ToQueryable<T>().ToWhere(where).ToInclude(includes).Select(select)
            .ToPaged(settings.Properties, settings.Ascending, settings.Index, settings.Size).ToListAsync().ConfigureAwait(false);
    }

    #endregion GetAll

    #region Add

    /// <inheritdoc/>
    public void Add(T item) => Context.Add(item);

    /// <inheritdoc/>
    public async Task AddAsync(T item) => await Context.AddAsync(item).ConfigureAwait(false);

    /// <inheritdoc/>
    public void AddRange(IEnumerable<T> items) => Context.AddRange(items);

    /// <inheritdoc/>
    public Task AddRangeAsync(IEnumerable<T> items) => Context.WithOptimized().Set<T>().AddRangeAsync(items);

    #endregion Add

    #region Update

    /// <inheritdoc/>
    public void Update(object key, T item) => UpdateItem(key, item);

    /// <inheritdoc/>
    public Task UpdateAsync(object key, T item)
    {
        Update(key, item);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void UpdatePartial(object key, object item) => UpdateItem(key, item);

    /// <inheritdoc/>
    public Task UpdatePartialAsync(object key, object item)
    {
        UpdatePartial(key, item);

        return Task.CompletedTask;
    }

    #endregion Update

    #region Delete

    /// <inheritdoc/>
    public void Delete(object key)
    {
        var item = GetByKey(key);

        if (item != default)
        {
            Context.Remove(item);
        }
    }

    /// <inheritdoc/>
    public void Delete(Expression<Func<T, bool>> where)
    {
        var items = Context.ToQueryable<T>().Where(where);

        if (items?.Any() == true)
        {
            Context.RemoveRange(items);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(object key)
    {
        var item = await GetByKeyAsync(key).ConfigureAwait(false);

        if (item != default)
        {
            Context.Remove(item);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Expression<Func<T, bool>> where)
    {
        var items = Context.ToQueryable<T>().Where(where);

        if (await items.AnyAsync().ConfigureAwait(false) == true)
        {
            Context.RemoveRange(items);
        }
    }

    #endregion Delete

    #region Any

    /// <inheritdoc/>
    public bool Any() => Context.ToQueryable<T>().Any();

    /// <inheritdoc/>
    public bool Any(Expression<Func<T, bool>> where) => Context.ToQueryable<T>().Any(where);

    /// <inheritdoc/>
    public Task<bool> AnyAsync() => Context.ToQueryable<T>().AnyAsync();

    /// <inheritdoc/>
    public Task<bool> AnyAsync(Expression<Func<T, bool>> where) => Context.ToQueryable<T>().AnyAsync(where);

    #endregion Any

    #region Count

    /// <inheritdoc/>
    public long Count() => Context.ToQueryable<T>().LongCount();

    /// <inheritdoc/>
    public long Count(Expression<Func<T, bool>> where) => Context.ToQueryable<T>().LongCount(where);

    /// <inheritdoc/>
    public Task<long> CountAsync() => Context.ToQueryable<T>().LongCountAsync();

    /// <inheritdoc/>
    public Task<long> CountAsync(Expression<Func<T, bool>> where) => Context.ToQueryable<T>().LongCountAsync(where);

    #endregion Count

    #region Private

    private static void UpdateReferences(EntityEntry<T> entry, object item)
    {
        foreach (var reference in entry.References)
        {
            var property = item.GetType().GetProperty(reference.Metadata.Name);

            if (property != default)
            {
                reference.CurrentValue = property.GetValue(item, default);
            }
        }
    }

    private void UpdateItem(object key, object item)
    {
        var entity = GetByKey(key);

        var entry = Context.Entry(entity.ToThrowIfNullOrEmpty());

        entry.CurrentValues.SetValues(item);

        UpdateReferences(entry, item);
    }

    #endregion
}