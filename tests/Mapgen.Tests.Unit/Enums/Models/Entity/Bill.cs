using System.Collections.Generic;

namespace Mapgen.Tests.Unit.Enums.Models.Entity;

public class Bill
{
  public required int Id { get; init; }
  public required BillStatus Status { get; init; }
  public required PaymentType? PaymentMethod { get; init; }
  public required List<BillStatus> StatusTransitions { get; init; } = [];
}

public enum BillStatus
{
  Draft,
  Sent,
  Paid,
  Overdue,
  Cancelled
}

public enum PaymentType
{
  CreditCard,
  BankTransfer,
  Cash,
  PayPal
}

