﻿using System;
using System.Collections;

namespace ClosedXML.Excel.CalcEngine
{
    internal class CellRangeReference : IValueObject, IEnumerable
    {
        public CellRangeReference(IXLRange range)
        {
            Range = range;
        }

        public IXLRange Range { get; }

        // ** IValueObject
        public object GetValue()
        {
            return GetValue(Range.FirstCell());
        }

        // ** IEnumerable
        public IEnumerator GetEnumerator()
        {
            if (Range.Worksheet.IsEmpty(XLCellsUsedOptions.AllContents))
                yield break;

            var lastCellAddress = Range.Worksheet.LastCellUsed().Address;
            var maxRow = Math.Min(Range.RangeAddress.LastAddress.RowNumber, lastCellAddress.RowNumber);
            var maxColumn = Math.Min(Range.RangeAddress.LastAddress.ColumnNumber, lastCellAddress.ColumnNumber);

            var trimmedRange = (XLRangeBase)Range.Worksheet
                .Range(
                    Range.FirstCell().Address,
                    new XLAddress(maxRow, maxColumn, fixedRow: false, fixedColumn: false)
                );

            foreach (var c in trimmedRange.CellValues())
                yield return c;
        }

        private Boolean _evaluating;

        // ** implementation
        private object GetValue(IXLCell cell)
        {
            if (_evaluating || (cell as XLCell).IsEvaluating)
            {
                throw new InvalidOperationException($"Circular Reference occured during evaluation. Cell: {cell.Address.ToString(XLReferenceStyle.Default, true)}");
            }
            try
            {
                _evaluating = true;
                var f = cell.FormulaA1;
                if (String.IsNullOrWhiteSpace(f))
                    return cell.Value;
                else
                {
                    return (cell as XLCell).Evaluate();
                }
            }
            finally
            {
                _evaluating = false;
            }
        }
    }
}
