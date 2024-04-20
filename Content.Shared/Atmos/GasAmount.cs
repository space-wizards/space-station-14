using Robust.Shared.Serialization;

namespace Content.Shared.Atmos;
//Time to reimplement shit that atmos already does because it's not in shared YIPPPEEEE!


//TODO: Remove this when gas mixture gets ported to shared
//This is basically just the data from gas mixture but in shared.

[DataDefinition, Serializable, NetSerializable]
public partial struct GasAmount(Gas gas, float mols)
{
    [DataField(required: true)]
    public Gas Gas = gas;
    [DataField(required: true)]
    public float Mols = mols;
};

[DataDefinition, Serializable, NetSerializable]
public partial struct SharedGasMixture
{
    [DataField(required: true)] private List<GasAmount> _gasAmounts = new();

    public IReadOnlyList<GasAmount> Gases => _gasAmounts;

    [DataField] public float Temperature = Atmospherics.T20C;
    [DataField("volume")] private float _volume = 0;
    private float _totalMols = 0;


    public float Volume
    {
        get => _volume;
        set
        {
            if (value < 0)
                value = 0;
            _volume = value;
        }
    }

    public float Pressure
    {
        get
        {
            if (Volume <= 0)
                return 0f;
            return _totalMols * Atmospherics.R * Temperature / Volume;
        }
    }


    public SharedGasMixture(){}

    public SharedGasMixture(List<GasAmount> gasAmounts, float volume = 0, float temperature = 0)
    {
        foreach (var gasAmt in gasAmounts)
        {
            AddGasAmount_Internal(gasAmt);
        }
        _volume = volume;
        Temperature = temperature;
    }

    public void AddGasAmount(GasAmount gasAmount)
    {
        var gasIndex = IndexOfGas(gasAmount.Gas);
        if (gasIndex >= 0)
        {
            UpdateGasMols_Internal(gasIndex, gasAmount.Mols);
            return;
        }
        AddGasAmount_Internal(gasAmount);
    }

    public void RemoveGasAmount(GasAmount gasAmount)
    {
        var gasIndex = IndexOfGas(gasAmount.Gas);
        if (gasIndex >= 0)
        {
            if (gasAmount.Mols >= _gasAmounts[gasIndex].Mols)
            {
                RemoveGasAmount_Internal(gasIndex);
                return;
            }
            UpdateGasMols_Internal(gasIndex, -gasAmount.Mols);
        }
    }

    public void SetGasAmount(GasAmount gasAmount)
    {
        var gasIndex = IndexOfGas(gasAmount.Gas);
        if (gasIndex >= 0)
        {
            if (gasAmount.Mols == 0)
            {
                RemoveGasAmount_Internal(gasIndex);
                return;
            }
            UpdateGasMols_Internal(gasIndex, gasAmount.Mols-_gasAmounts[gasIndex].Mols);
            return;
        }
        AddGasAmount_Internal(gasAmount);
    }

    private void AddGasAmount_Internal(GasAmount gasAmount)
    {
        _gasAmounts.Add(gasAmount);
        _totalMols += gasAmount.Mols;
    }

    private void RemoveGasAmount_Internal(int gasIndex)
    {
        _totalMols -= _gasAmounts[gasIndex].Mols;
        _gasAmounts.RemoveAt(gasIndex);
    }

    private void UpdateGasMols_Internal(int gasIndex, float molsToAdd)
    {
        _gasAmounts[gasIndex] = _gasAmounts[gasIndex] with { Mols = _gasAmounts[gasIndex].Mols + molsToAdd };
        _totalMols += molsToAdd;
    }


    private int IndexOfGas(Gas gas)
    {
        var index = 0;
        foreach (var gasAmt in _gasAmounts)
        {
            if (gasAmt.Gas == gas)
                return index;
            index++;
        }
        return -1;
    }

    public bool TryAddGasAmount(GasAmount gasAmount)
    {
        if (HasGas(gasAmount.Gas))
            return false;
        AddGasAmount_Internal(gasAmount);
        return true;
    }


    public bool TryGetGasAmount(Gas gas, out float amount)
    {
        amount = 0;
        foreach (var gasAmt in _gasAmounts)
        {
            if (gasAmt.Gas == gas)
            {
                amount = gasAmt.Mols;
                return true;
            }
        }
        return false;
    }

    public bool HasGas(Gas gas)
    {
        return TryGetGasAmount(gas, out _);
    }



};
