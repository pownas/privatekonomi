using Microsoft.EntityFrameworkCore;
using Moq;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class BillServiceTests : IDisposable
{
    private readonly PrivatekonomyContext _context;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly BillService _service;
    private const string UserId = "test-user-id";

    public BillServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PrivatekonomyContext(options);
        _mockAuditLogService = new Mock<IAuditLogService>();

        _service = new BillService(_context, _mockAuditLogService.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    public void Dispose()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }

    [TestMethod]
    public async Task CreateBillAsync_ValidBill_ReturnsBillWithId()
    {
        // Arrange
        var bill = new Bill
        {
            UserId = UserId,
            Name = "Elräkning",
            Amount = 500m,
            Currency = "SEK",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            Status = "Pending"
        };

        // Act
        var result = await _service.CreateBillAsync(bill);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.BillId > 0);
        Assert.AreEqual("Elräkning", result.Name);
        Assert.AreEqual(500m, result.Amount);
    }

    [TestMethod]
    public async Task GetBillsAsync_ReturnsOnlyUserBills()
    {
        // Arrange
        var bill1 = new Bill { UserId = UserId, Name = "Hyra", Amount = 8000m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(5), Status = "Pending" };
        var bill2 = new Bill { UserId = UserId, Name = "El", Amount = 500m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(10), Status = "Pending" };
        var bill3 = new Bill { UserId = "other-user", Name = "Vatten", Amount = 200m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(15), Status = "Pending" };

        _context.Bills.AddRange(bill1, bill2, bill3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetBillsAsync(UserId);

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.All(b => b.UserId == UserId));
    }

    [TestMethod]
    public async Task GetBillByIdAsync_ExistingBill_ReturnsBill()
    {
        // Arrange
        var bill = new Bill { UserId = UserId, Name = "Internet", Amount = 300m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(14), Status = "Pending" };
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetBillByIdAsync(bill.BillId, UserId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Internet", result.Name);
    }

    [TestMethod]
    public async Task GetBillByIdAsync_WrongUser_ReturnsNull()
    {
        // Arrange
        var bill = new Bill { UserId = "other-user", Name = "Telefon", Amount = 200m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(7), Status = "Pending" };
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetBillByIdAsync(bill.BillId, UserId);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetPendingBillsAsync_ReturnsOnlyPendingBills()
    {
        // Arrange
        var pending = new Bill { UserId = UserId, Name = "Hyra", Amount = 8000m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(5), Status = "Pending" };
        var paid = new Bill { UserId = UserId, Name = "El", Amount = 500m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(-5), Status = "Paid", PaidDate = DateTime.UtcNow };

        _context.Bills.AddRange(pending, paid);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPendingBillsAsync(UserId);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Pending", result[0].Status);
    }

    [TestMethod]
    public async Task GetOverdueBillsAsync_ReturnsOnlyOverduePendingBills()
    {
        // Arrange
        var overdue = new Bill { UserId = UserId, Name = "Hyra", Amount = 8000m, Currency = "SEK", IssueDate = DateTime.UtcNow.AddDays(-60), DueDate = DateTime.UtcNow.AddDays(-5), Status = "Pending" };
        var upcoming = new Bill { UserId = UserId, Name = "El", Amount = 500m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(10), Status = "Pending" };

        _context.Bills.AddRange(overdue, upcoming);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetOverdueBillsAsync(UserId);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result[0].DueDate < DateTime.UtcNow.Date);
    }

    [TestMethod]
    public async Task GetBillsDueSoonAsync_ReturnsOnlyBillsWithinRange()
    {
        // Arrange
        var soon = new Bill { UserId = UserId, Name = "Hyra", Amount = 8000m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(3), Status = "Pending" };
        var later = new Bill { UserId = UserId, Name = "El", Amount = 500m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(15), Status = "Pending" };

        _context.Bills.AddRange(soon, later);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetBillsDueSoonAsync(UserId, 7);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Hyra", result[0].Name);
    }

    [TestMethod]
    public async Task UpdateBillAsync_ValidUpdate_UpdatesFields()
    {
        // Arrange
        var bill = new Bill { UserId = UserId, Name = "Hyra", Amount = 8000m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(5), Status = "Pending" };
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        // Act
        bill.Amount = 8500m;
        bill.Name = "Hyra (uppdaterad)";
        var result = await _service.UpdateBillAsync(bill);

        // Assert
        Assert.AreEqual(8500m, result.Amount);
        Assert.AreEqual("Hyra (uppdaterad)", result.Name);
        Assert.IsNotNull(result.UpdatedAt);
    }

    [TestMethod]
    public async Task DeleteBillAsync_ExistingBill_RemovesBill()
    {
        // Arrange
        var bill = new Bill { UserId = UserId, Name = "Hyra", Amount = 8000m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(5), Status = "Pending" };
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();
        var billId = bill.BillId;

        // Act
        await _service.DeleteBillAsync(billId, UserId);

        // Assert
        var deleted = await _context.Bills.FindAsync(billId);
        Assert.IsNull(deleted);
    }

    [TestMethod]
    public async Task MarkBillAsPaidAsync_ExistingBill_MarksAsPaid()
    {
        // Arrange
        var bill = new Bill { UserId = UserId, Name = "Hyra", Amount = 8000m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(5), Status = "Pending" };
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        var paidDate = DateTime.UtcNow.Date;

        // Act
        await _service.MarkBillAsPaidAsync(bill.BillId, paidDate);

        // Assert
        var updated = await _context.Bills.FindAsync(bill.BillId);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Paid", updated.Status);
        Assert.AreEqual(paidDate, updated.PaidDate);
    }

    [TestMethod]
    public async Task MarkBillAsPaidAsync_NonExistentBill_ThrowsInvalidOperationException()
    {
        // Act & Assert
        bool exceptionThrown = false;
        try
        {
            await _service.MarkBillAsPaidAsync(9999, DateTime.UtcNow);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }
        Assert.IsTrue(exceptionThrown);
    }

    [TestMethod]
    public async Task AddReminderAsync_ValidBillId_AddsReminder()
    {
        // Arrange
        var bill = new Bill { UserId = UserId, Name = "Hyra", Amount = 8000m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(5), Status = "Pending" };
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        var reminderDate = DateTime.UtcNow.AddDays(2);

        // Act
        await _service.AddReminderAsync(bill.BillId, reminderDate, "Notification");

        // Assert
        var reminders = await _context.BillReminders.Where(r => r.BillId == bill.BillId).ToListAsync();
        Assert.AreEqual(1, reminders.Count);
        Assert.AreEqual(reminderDate, reminders[0].ReminderDate);
        Assert.AreEqual("Notification", reminders[0].ReminderMethod);
        Assert.IsFalse(reminders[0].IsSent);
    }

    [TestMethod]
    public async Task MarkReminderAsSentAsync_ExistingReminder_MarksSent()
    {
        // Arrange
        var bill = new Bill { UserId = UserId, Name = "Hyra", Amount = 8000m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(5), Status = "Pending" };
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        var reminder = new BillReminder { BillId = bill.BillId, ReminderDate = DateTime.UtcNow.AddDays(1), IsSent = false };
        _context.BillReminders.Add(reminder);
        await _context.SaveChangesAsync();

        // Act
        await _service.MarkReminderAsSentAsync(reminder.BillReminderId);

        // Assert
        var updated = await _context.BillReminders.FindAsync(reminder.BillReminderId);
        Assert.IsNotNull(updated);
        Assert.IsTrue(updated.IsSent);
        Assert.IsNotNull(updated.SentDate);
    }

    [TestMethod]
    public async Task GetPendingRemindersAsync_ReturnsOnlyUnsentDueReminders()
    {
        // Arrange
        var bill = new Bill { UserId = UserId, Name = "Hyra", Amount = 8000m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(5), Status = "Pending" };
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        var duePast = new BillReminder { BillId = bill.BillId, ReminderDate = DateTime.UtcNow.AddMinutes(-10), IsSent = false };
        var alreadySent = new BillReminder { BillId = bill.BillId, ReminderDate = DateTime.UtcNow.AddMinutes(-20), IsSent = true };
        var upcoming = new BillReminder { BillId = bill.BillId, ReminderDate = DateTime.UtcNow.AddDays(1), IsSent = false };

        _context.BillReminders.AddRange(duePast, alreadySent, upcoming);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPendingRemindersAsync();

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.IsFalse(result[0].IsSent);
        Assert.IsTrue(result[0].ReminderDate <= DateTime.UtcNow);
    }

    [TestMethod]
    public async Task GetBillsByHouseholdAsync_ReturnsOnlyHouseholdBills()
    {
        // Arrange
        var householdId = 1;
        var bill1 = new Bill { UserId = UserId, Name = "Hyra", Amount = 8000m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(5), Status = "Pending", HouseholdId = householdId };
        var bill2 = new Bill { UserId = UserId, Name = "El", Amount = 500m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(10), Status = "Pending" };

        _context.Bills.AddRange(bill1, bill2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetBillsByHouseholdAsync(householdId, UserId);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(householdId, result[0].HouseholdId);
    }

    [TestMethod]
    public async Task CreateScheduleAsync_ValidSchedule_ReturnsScheduleWithId()
    {
        // Arrange
        var bill = new Bill { UserId = UserId, Name = "Hyra", Amount = 8000m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(30), Status = "Pending", IsRecurring = true };
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        var schedule = new BillSchedule
        {
            BillId = bill.BillId,
            Frequency = "Monthly",
            StartDate = DateTime.UtcNow.Date,
            NextOccurrenceDate = DateTime.UtcNow.Date.AddMonths(1),
            IsActive = true,
            ReminderDaysBefore = 3
        };

        // Act
        var result = await _service.CreateScheduleAsync(schedule);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.BillScheduleId > 0);
        Assert.AreEqual("Monthly", result.Frequency);
    }

    [TestMethod]
    public async Task GetScheduleByBillIdAsync_ExistingSchedule_ReturnsSchedule()
    {
        // Arrange
        var bill = new Bill { UserId = UserId, Name = "Hyra", Amount = 8000m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(30), Status = "Pending", IsRecurring = true };
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        var schedule = new BillSchedule
        {
            BillId = bill.BillId,
            Frequency = "Monthly",
            StartDate = DateTime.UtcNow.Date,
            NextOccurrenceDate = DateTime.UtcNow.Date.AddMonths(1),
            IsActive = true
        };
        _context.BillSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetScheduleByBillIdAsync(bill.BillId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(bill.BillId, result.BillId);
        Assert.AreEqual("Monthly", result.Frequency);
    }

    [TestMethod]
    public async Task GetScheduleByBillIdAsync_NoSchedule_ReturnsNull()
    {
        // Act
        var result = await _service.GetScheduleByBillIdAsync(9999);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetDueSchedulesAsync_ReturnsOnlyDueActiveSchedules()
    {
        // Arrange
        var bill = new Bill { UserId = UserId, Name = "Hyra", Amount = 8000m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(30), Status = "Pending", IsRecurring = true };
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        var dueSchedule = new BillSchedule
        {
            BillId = bill.BillId,
            Frequency = "Monthly",
            StartDate = DateTime.UtcNow.Date.AddMonths(-1),
            NextOccurrenceDate = DateTime.UtcNow.Date.AddDays(-1), // due yesterday
            IsActive = true
        };
        var futureSchedule = new BillSchedule
        {
            BillId = bill.BillId,
            Frequency = "Monthly",
            StartDate = DateTime.UtcNow.Date,
            NextOccurrenceDate = DateTime.UtcNow.Date.AddDays(15), // not due yet
            IsActive = true
        };
        var inactiveSchedule = new BillSchedule
        {
            BillId = bill.BillId,
            Frequency = "Monthly",
            StartDate = DateTime.UtcNow.Date.AddMonths(-2),
            NextOccurrenceDate = DateTime.UtcNow.Date.AddDays(-2),
            IsActive = false // inactive
        };

        _context.BillSchedules.AddRange(dueSchedule, futureSchedule, inactiveSchedule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDueSchedulesAsync();

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(dueSchedule.BillScheduleId, result[0].BillScheduleId);
    }

    [TestMethod]
    public async Task GenerateNextOccurrenceAsync_MonthlySchedule_CreatesNewBillAndAdvancesSchedule()
    {
        // Arrange
        var bill = new Bill
        {
            UserId = UserId,
            Name = "Hyra",
            Amount = 8000m,
            Currency = "SEK",
            IssueDate = DateTime.UtcNow.Date.AddMonths(-1),
            DueDate = DateTime.UtcNow.Date.AddDays(-1),
            Status = "Paid",
            IsRecurring = true,
            RecurringFrequency = "Monthly",
            Payee = "Hyresvärden"
        };
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        var occurrenceDate = DateTime.UtcNow.Date;
        var schedule = new BillSchedule
        {
            BillId = bill.BillId,
            Frequency = "Monthly",
            StartDate = DateTime.UtcNow.Date.AddMonths(-1),
            NextOccurrenceDate = occurrenceDate,
            IsActive = true,
            ReminderDaysBefore = 0 // no reminder so we don't need to track that
        };
        _context.BillSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        // Act
        var newBill = await _service.GenerateNextOccurrenceAsync(schedule);

        // Assert
        Assert.IsNotNull(newBill);
        Assert.IsTrue(newBill.BillId > 0);
        Assert.AreEqual("Hyra", newBill.Name);
        Assert.AreEqual(8000m, newBill.Amount);
        Assert.AreEqual("Pending", newBill.Status);
        Assert.AreEqual(occurrenceDate, newBill.DueDate);

        // Verify schedule was advanced
        var updatedSchedule = await _context.BillSchedules.FindAsync(schedule.BillScheduleId);
        Assert.IsNotNull(updatedSchedule);
        Assert.AreEqual(occurrenceDate.AddMonths(1), updatedSchedule.NextOccurrenceDate);
    }

    [TestMethod]
    public async Task UpdateScheduleAsync_ValidUpdate_UpdatesFields()
    {
        // Arrange
        var bill = new Bill { UserId = UserId, Name = "El", Amount = 500m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(30), Status = "Pending", IsRecurring = true };
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        var schedule = new BillSchedule
        {
            BillId = bill.BillId,
            Frequency = "Monthly",
            StartDate = DateTime.UtcNow.Date,
            NextOccurrenceDate = DateTime.UtcNow.Date.AddMonths(1),
            IsActive = true,
            ReminderDaysBefore = 3
        };
        _context.BillSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        // Act
        schedule.ReminderDaysBefore = 5;
        schedule.Frequency = "Quarterly";
        var result = await _service.UpdateScheduleAsync(schedule);

        // Assert
        Assert.AreEqual(5, result.ReminderDaysBefore);
        Assert.AreEqual("Quarterly", result.Frequency);
        Assert.IsNotNull(result.UpdatedAt);
    }

    [TestMethod]
    public async Task DeleteScheduleAsync_ExistingSchedule_RemovesSchedule()
    {
        // Arrange
        var bill = new Bill { UserId = UserId, Name = "El", Amount = 500m, Currency = "SEK", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(30), Status = "Pending", IsRecurring = true };
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        var schedule = new BillSchedule
        {
            BillId = bill.BillId,
            Frequency = "Monthly",
            StartDate = DateTime.UtcNow.Date,
            NextOccurrenceDate = DateTime.UtcNow.Date.AddMonths(1),
            IsActive = true
        };
        _context.BillSchedules.Add(schedule);
        await _context.SaveChangesAsync();
        var scheduleId = schedule.BillScheduleId;

        // Act
        await _service.DeleteScheduleAsync(scheduleId);

        // Assert
        var deleted = await _context.BillSchedules.FindAsync(scheduleId);
        Assert.IsNull(deleted);
    }

    [TestMethod]
    public async Task GenerateNextOccurrenceAsync_WithReminderDaysBefore_CreatesReminder()
    {
        // Arrange
        var futureDueDate = DateTime.UtcNow.Date.AddDays(10);
        var bill = new Bill
        {
            UserId = UserId,
            Name = "Internet",
            Amount = 300m,
            Currency = "SEK",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = futureDueDate,
            Status = "Pending",
            IsRecurring = true,
            RecurringFrequency = "Monthly"
        };
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        var schedule = new BillSchedule
        {
            BillId = bill.BillId,
            Frequency = "Monthly",
            StartDate = DateTime.UtcNow.Date,
            NextOccurrenceDate = futureDueDate,
            IsActive = true,
            ReminderDaysBefore = 3
        };
        _context.BillSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        // Act
        var newBill = await _service.GenerateNextOccurrenceAsync(schedule);

        // Assert - reminder should be created 3 days before due date
        var reminders = await _context.BillReminders
            .Where(r => r.BillId == newBill.BillId)
            .ToListAsync();
        Assert.AreEqual(1, reminders.Count);
        Assert.AreEqual(futureDueDate.AddDays(-3), reminders[0].ReminderDate);
        Assert.IsFalse(reminders[0].IsSent);
    }
}
