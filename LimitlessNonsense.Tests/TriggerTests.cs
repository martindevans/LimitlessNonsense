using LimitlessNonsense.ContextManagement;
using System.Text.Json;

namespace LimitlessNonsense.Tests;

[TestClass]
public sealed class TriggerTests
{
    #region Always

    [TestMethod]
    public void Always_TriggerDelay_ReturnsZero()
    {
        var trigger = Trigger.Always();

        var delay = trigger.TriggerDelay(DateTime.UtcNow);

        Assert.AreEqual(TimeSpan.Zero, delay);
    }

    [TestMethod]
    public void Always_TriggerDelay_IsIndependentOfNow()
    {
        var trigger = Trigger.Always();

        Assert.AreEqual(TimeSpan.Zero, trigger.TriggerDelay(DateTime.MinValue));
        Assert.AreEqual(TimeSpan.Zero, trigger.TriggerDelay(DateTime.MaxValue));
    }

    #endregion

    #region Never

    [TestMethod]
    public void Never_TriggerDelay_ReturnsNull()
    {
        var trigger = Trigger.Never();

        var delay = trigger.TriggerDelay(DateTime.UtcNow);

        Assert.IsNull(delay);
    }

    [TestMethod]
    public void Never_TriggerDelay_IsIndependentOfNow()
    {
        var trigger = Trigger.Never();

        Assert.IsNull(trigger.TriggerDelay(DateTime.MinValue));
        Assert.IsNull(trigger.TriggerDelay(DateTime.MaxValue));
    }

    #endregion

    #region Idle

    [TestMethod]
    public void Idle_TriggerDelay_ReturnsDuration()
    {
        var duration = TimeSpan.FromMinutes(30);
        var trigger = Trigger.Idle(duration);

        var delay = trigger.TriggerDelay(DateTime.UtcNow);

        Assert.AreEqual(duration, delay);
    }

    [TestMethod]
    public void Idle_TriggerDelay_WithZeroDuration_ReturnsZero()
    {
        var trigger = Trigger.Idle(TimeSpan.Zero);

        var delay = trigger.TriggerDelay(DateTime.UtcNow);

        Assert.AreEqual(TimeSpan.Zero, delay);
    }

    [TestMethod]
    public void Idle_TriggerDelay_WithNegativeDuration_ReturnsNegative()
    {
        var duration = TimeSpan.FromMinutes(-5);
        var trigger = Trigger.Idle(duration);

        var delay = trigger.TriggerDelay(DateTime.UtcNow);

        Assert.AreEqual(duration, delay);
    }

    [TestMethod]
    public void Idle_TriggerDelay_IsIndependentOfNow()
    {
        var duration = TimeSpan.FromHours(1);
        var trigger = Trigger.Idle(duration);

        Assert.AreEqual(duration, trigger.TriggerDelay(DateTime.MinValue));
        Assert.AreEqual(duration, trigger.TriggerDelay(DateTime.MaxValue));
    }

    #endregion

    #region Schedule

    [TestMethod]
    [DataRow(15, 0, 14, 0, 1)]   // scheduled 15:00, now 14:00 → 1 hour delay
    [DataRow(14, 0, 15, 0, 23)]  // scheduled 14:00, now 15:00 → 23 hours until next occurrence
    [DataRow(14, 0, 14, 0, 0)]   // scheduled 14:00, now 14:00 → triggers immediately
    [DataRow( 0, 0, 23, 0, 1)]   // scheduled 00:00, now 23:00 → 1 hour (midnight rollover)
    public void Schedule_TriggerDelay_ReturnsExpectedDelay(int schedHour, int schedMin, int nowHour, int nowMin, int expectedHours)
    {
        var scheduledTime = new TimeOnly(schedHour, schedMin);
        var now = new DateTime(2024, 1, 1, nowHour, nowMin, 0);
        var trigger = Trigger.Schedule(scheduledTime);

        var delay = trigger.TriggerDelay(now);

        Assert.AreEqual(TimeSpan.FromHours(expectedHours), delay);
    }

    #endregion

    #region JSON serialization

    [TestMethod]
    public void Always_SerializesWithCorrectDiscriminator()
    {
        var trigger = Trigger.Always();

        var json = JsonSerializer.Serialize(trigger);

        StringAssert.Contains(json, "\"$type\":\"Always\"");
    }

    [TestMethod]
    public void Always_RoundTrips_ThroughJson()
    {
        var trigger = Trigger.Always();

        var json = JsonSerializer.Serialize(trigger);
        var deserialized = JsonSerializer.Deserialize<Trigger>(json);

        Assert.AreEqual(trigger, deserialized);
    }

    [TestMethod]
    public void Idle_SerializesWithCorrectDiscriminator()
    {
        var trigger = Trigger.Idle(TimeSpan.FromMinutes(15));

        var json = JsonSerializer.Serialize(trigger);

        StringAssert.Contains(json, "\"$type\":\"Idle\"");
    }

    [TestMethod]
    public void Idle_RoundTrips_ThroughJson()
    {
        var trigger = Trigger.Idle(TimeSpan.FromMinutes(15));

        var json = JsonSerializer.Serialize(trigger);
        var deserialized = JsonSerializer.Deserialize<Trigger>(json);

        Assert.AreEqual(trigger, deserialized);
    }

    [TestMethod]
    public void Schedule_SerializesWithCorrectDiscriminator()
    {
        var trigger = Trigger.Schedule(new TimeOnly(9, 30, 0));

        var json = JsonSerializer.Serialize(trigger);

        StringAssert.Contains(json, "\"$type\":\"Schedule\"");
    }

    [TestMethod]
    public void Schedule_RoundTrips_ThroughJson()
    {
        var trigger = Trigger.Schedule(new TimeOnly(9, 30, 0));

        var json = JsonSerializer.Serialize(trigger);
        var deserialized = JsonSerializer.Deserialize<Trigger>(json);

        Assert.AreEqual(trigger, deserialized);
    }

    [TestMethod]
    public void Never_SerializesWithCorrectDiscriminator()
    {
        var trigger = Trigger.Never();

        var json = JsonSerializer.Serialize(trigger);

        StringAssert.Contains(json, "\"$type\":\"Never\"");
    }

    [TestMethod]
    public void Never_RoundTrips_ThroughJson()
    {
        var trigger = Trigger.Never();

        var json = JsonSerializer.Serialize(trigger);
        var deserialized = JsonSerializer.Deserialize<Trigger>(json);

        Assert.AreEqual(trigger, deserialized);
    }

    #endregion
}
