using System;
using System.Collections.Generic;
using System.Linq;
using FastTreeDataGrid.ExcelDemo.Models;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace FastTreeDataGrid.ExcelDemo.Services;

public sealed class PowerFxFormulaEvaluator
{
    private readonly RecalcEngine _engine;
    private readonly RecordType _recordType;
    private readonly IReadOnlyList<MeasureOption> _measures;
    private readonly IReadOnlyList<FormulaDefinition> _formulaList;
    private readonly Dictionary<string, FormulaDefinition> _formulaLookup;

    public PowerFxFormulaEvaluator(IReadOnlyList<MeasureOption> measures, IEnumerable<FormulaDefinition> formulas)
    {
        _measures = measures ?? throw new ArgumentNullException(nameof(measures));
        _engine = new RecalcEngine();

        var recordType = RecordType.Empty();
        foreach (var measure in _measures)
        {
            recordType = recordType.Add(new NamedFormulaType(measure.Key, FormulaType.Number));
        }

        _recordType = recordType;
        _formulaList = (formulas ?? Array.Empty<FormulaDefinition>()).ToList();
        _formulaLookup = new Dictionary<string, FormulaDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var formula in _formulaList)
        {
            _formulaLookup[formula.Key] = formula;
            var check = _engine.Check(formula.Expression, _recordType);
            if (!check.IsSuccess)
            {
                var reasons = string.Join(Environment.NewLine, check.Errors.Select(e => e.Message));
                throw new ArgumentException($"Formula '{formula.Key}' is invalid:{Environment.NewLine}{reasons}");
            }
        }
    }

    public FormulaDefinition? GetDefinition(string key)
    {
        if (key is null)
        {
            return null;
        }

        return _formulaLookup.TryGetValue(key, out var definition) ? definition : null;
    }

    public double? Evaluate(string formulaKey, double[] measureValues)
    {
        if (formulaKey is null || !_formulaLookup.TryGetValue(formulaKey, out var definition))
        {
            return null;
        }

        var scope = CreateScope(measureValues);
        return EvaluateInternal(definition, scope);
    }

    public void EvaluateAll(double[] measureValues, double?[] destination)
    {
        if (_formulaList.Count == 0 || destination is null || destination.Length == 0)
        {
            return;
        }

        if (destination.Length < _formulaList.Count)
        {
            throw new ArgumentException("Destination array is too small to hold all formula results.", nameof(destination));
        }

        var scope = CreateScope(measureValues);
        for (var i = 0; i < _formulaList.Count; i++)
        {
            destination[i] = EvaluateInternal(_formulaList[i], scope);
        }
    }

    private RecordValue CreateScope(double[] measureValues)
    {
        var fields = new NamedValue[_measures.Count];

        for (var i = 0; i < _measures.Count; i++)
        {
            double? value = null;
            if (measureValues is not null && i < measureValues.Length)
            {
                var candidate = measureValues[i];
                if (double.IsFinite(candidate))
                {
                    value = candidate;
                }
            }

            fields[i] = value.HasValue
                ? new NamedValue(_measures[i].Key, FormulaValue.New(value.Value))
                : new NamedValue(_measures[i].Key, FormulaValue.NewBlank(FormulaType.Number));
        }

        return FormulaValue.NewRecordFromFields(fields);
    }

    private double? EvaluateInternal(FormulaDefinition definition, RecordValue scope)
    {
        var result = _engine.Eval(definition.Expression, scope);
        return ConvertResult(result);
    }

    private static double? ConvertResult(FormulaValue result)
    {
        return result switch
        {
            NumberValue number => number.Value,
            DecimalValue dec => (double)dec.Value,
            BooleanValue booleanValue => booleanValue.Value ? 1d : 0d,
            BlankValue => null,
            _ => null,
        };
    }
}
