namespace BuildingBlocks.Health;

public sealed class ReadinessState
{
    private volatile bool ready;

    public bool IsReady => ready;

    public void MarkReady() => ready = true;

    public void MarkNotReady() => ready = false;
}
