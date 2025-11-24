using System;

public static class TutorialEventHub
{
    public static event Action CameraMoved;
    public static void RaiseCameraMoved() => CameraMoved?.Invoke();

    public static event Action TilePlaced;
    public static void RaiseTilePlaced() => TilePlaced?.Invoke();

    public static event Action TurretPlaced;
    public static void RaiseTurretPlaced() => TurretPlaced?.Invoke();

    public static event Action WaveStarted;
    public static void RaiseWaveStarted() => WaveStarted?.Invoke();

    public static event Action CoreDamaged;
    public static void RaiseCoreDamaged() => CoreDamaged?.Invoke();

    public static event Action<string> CustomStepCompleted;
    public static void RaiseCustomStepCompleted(string stepId) => CustomStepCompleted?.Invoke(stepId);

    public static event Action WorldSwitched;
    public static void RaiseWorldSwitched() => WorldSwitched?.Invoke();
}
