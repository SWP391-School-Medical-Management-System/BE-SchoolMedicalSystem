namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalItemResponse;

public class MedicalItemStockSummaryResponse
{
    public int TotalItems { get; set; }
    public int LowStockItems { get; set; }
    public int ExpiredItems { get; set; }
    public int ExpiringSoonItems { get; set; }
    public int MedicationCount { get; set; }
    public int SupplyCount { get; set; }
}