namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalItemUsageResponse;

public class MedicalItemUsageResponse
{
    public Guid Id { get; set; }
    public Guid MedicalItemId { get; set; }
    public Guid HealthEventId { get; set; }
    public double Quantity { get; set; }
    public string Notes { get; set; }
    public DateTime UsedAt { get; set; }
    public Guid UsedById { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string MedicalItemName { get; set; }
    public string MedicalItemType { get; set; }
    public string Unit { get; set; }
    public string HealthEventDescription { get; set; }
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    public string UsedByName { get; set; }
    public bool IsCorrection { get; set; }
    public bool IsReturn { get; set; }
    public string UsageType { get; set; } // "Normal", "Correction", "Return"
    public string QuantityDisplay => $"{Math.Abs(Quantity)} {Unit}";

    public string UsageTypeDisplay => UsageType switch
    {
        "Correction" => "Điều chỉnh",
        "Return" => "Hoàn trả",
        _ => "Sử dụng"
    };
}