using System.Text.Json.Serialization;

namespace LimitlessNonsense.ContextManagement;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(Always), nameof(ContextManagement.Always))]
[JsonDerivedType(typeof(Idle), nameof(ContextManagement.Idle))]
[JsonDerivedType(typeof(Schedule), nameof(ContextManagement.Schedule))]
public abstract record Trigger
{
    #region static factories
    /// <summary>
    /// Always trigger
    /// </summary>
    /// <returns></returns>
    public static Trigger Always()
    {
        return new Always();
    }

    /// <summary>
    /// Never trigger
    /// </summary>
    /// <returns></returns>
    public static Trigger Never()
    {
        return new Never();
    }

    /// <summary>
    /// Trigger after a certain amount of idle time
    /// </summary>
    /// <param name="duration"></param>
    /// <returns></returns>
    public static Trigger Idle(TimeSpan duration)
    {
        return new Idle(duration);
    }

    /// <summary>
    /// Trigger once a day at a set time
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public static Trigger Schedule(TimeOnly time)
    {
        return new Schedule(time);
    }
    #endregion

    /// <summary>
    /// Get the time until this trigger would next activate. Return zero or negative time to activate immediately. Return null to indicate never triggering.
    /// </summary>
    /// <param name="now"></param>
    /// <returns></returns>
    protected internal abstract TimeSpan? TriggerDelay(DateTime now);
}

internal record Always
    : Trigger
{
    protected internal override TimeSpan? TriggerDelay(DateTime now)
    {
        return TimeSpan.Zero;
    }
}

internal record Never
    : Trigger
{
    protected internal override TimeSpan? TriggerDelay(DateTime now)
    {
        return null;
    }
}

internal record Idle(TimeSpan Duration)
    : Trigger
{
    protected internal override TimeSpan? TriggerDelay(DateTime now)
    {
        return Duration;
    }
}

internal record Schedule(TimeOnly Time)
    : Trigger
{
    protected internal override TimeSpan? TriggerDelay(DateTime now)
    {
        var nowTime = TimeOnly.FromDateTime(now);

        var next = Time - nowTime;
        if (next < TimeSpan.Zero)
            next += TimeSpan.FromDays(1);

        return next;
    }
}