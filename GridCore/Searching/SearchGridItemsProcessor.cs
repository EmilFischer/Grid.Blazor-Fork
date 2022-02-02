﻿using GridShared.Columns;
using GridShared.Searching;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace GridCore.Searching
{
    /// <summary>
    ///     Settings grid items, based on current searching settings
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class SearchGridItemsProcessor<T> : IGridItemsProcessor<T>
    {
        private readonly ISGrid _grid;
        private IGridSearchSettings _settings;

        public SearchGridItemsProcessor(ISGrid grid, IGridSearchSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException("settings");
            _grid = grid;
            _settings = settings;
        }

        public void UpdateSettings(IGridSearchSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException("settings");
            _settings = settings;
        }

        #region IGridItemsProcessor<T> Members

        public IQueryable<T> Process(IQueryable<T> items)
        {
            if (_grid.SearchingEnabled && !string.IsNullOrWhiteSpace(_settings.SearchValue))
            {
                ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
                Expression binaryExpression = null;

                if (_grid.SearchingSplittedWords)
                {
                    var searchWords = _settings.SearchValue.Split(' ');
                    foreach (var searchWord in searchWords)
                    {
                        binaryExpression = GetExpression(_grid, binaryExpression, parameter, searchWord);
                    }
                }
                else
                    binaryExpression = GetExpression(_grid, binaryExpression, parameter, _settings.SearchValue);

                // apply extension to items
                if (binaryExpression != null)
                {
                    return items.Where(Expression.Lambda<Func<T, bool>>(binaryExpression, parameter));
                }
            }
            return items;         
        }

        private Expression GetExpression(ISGrid grid, Expression binaryExpression, ParameterExpression parameter, string searchValue)
        {
            foreach (IGridColumn column in grid.Columns)
            {
                IGridColumn<T> gridColumn = column as IGridColumn<T>;
                if (gridColumn == null) continue;
                if (gridColumn.Search == null) continue;
                if (!grid.SearchingHiddenColumns && gridColumn.Hidden) continue;

                if (binaryExpression == null)
                {
                    binaryExpression = gridColumn.Search.GetExpression(searchValue,
                        grid.SearchingOnlyTextColumns, parameter, grid.RemoveDiacritics);
                }
                else
                {
                    Expression expression = gridColumn.Search.GetExpression(searchValue,
                        grid.SearchingOnlyTextColumns, parameter, grid.RemoveDiacritics);
                    if (expression != null)
                    {
                        binaryExpression = Expression.OrElse(binaryExpression, expression);
                    }
                }
            }
            return binaryExpression;
        }

        #endregion
    }
}
