using System.Collections.Generic;
using Verse;

namespace NoLightningFires.DevHarness;

public enum ScenarioStatus
{
    Running,
    Passed,
    Failed
}

public abstract class HarnessScenario
{
    public abstract string Name { get; }

    public virtual int TimeoutTicks => 5000;

    /// <summary>Game speed while this scenario runs.</summary>
    public virtual TimeSpeed Speed => TimeSpeed.Ultrafast;

    /// <summary>
    /// True for interactive setups (not tests): after a successful Setup the runner hands control back to the
    /// player, never ticks the scenario, records no result and does not shut the game down.
    /// </summary>
    public virtual bool IsPersistent => false;

    /// <summary>Name/value measurements written to results.jsonl (not failures).</summary>
    public readonly List<string> Metrics = new List<string>();

    /// <summary>Called once. <paramref name="area"/> is a cleared, unroofed, unfogged 26x26 rectangle.</summary>
    public abstract void Setup(Map map, CellRect area);

    /// <summary>Called every game tick after Setup. Add readable messages to <paramref name="failures"/>.</summary>
    public abstract ScenarioStatus Tick(Map map, int ticksSinceSetup, List<string> failures);

    /// <summary>Called once after the scenario finishes (pass or fail).</summary>
    public virtual void Cleanup(Map map)
    {
    }
}
