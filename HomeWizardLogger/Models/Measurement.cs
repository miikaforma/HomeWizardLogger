using System.Text.Json.Serialization;

namespace HomeWizardLogger.Models
{
    public class Measurement
    {
        [JsonPropertyName("unique_id")] public string? UniqueId { get; set; }
        [JsonPropertyName("protocol_version")] public double? ProtocolVersion { get; set; }
        [JsonPropertyName("meter_model")] public string? MeterModel { get; set; }
        [JsonPropertyName("timestamp")] public DateTime? Timestamp { get; set; }
        [JsonPropertyName("tariff")] public double? Tariff { get; set; }
        [JsonPropertyName("energy_import_kwh")] public double? EnergyImportKwh { get; set; }
        [JsonPropertyName("energy_import_t1_kwh")] public double? EnergyImportT1Kwh { get; set; }
        [JsonPropertyName("energy_import_t2_kwh")] public double? EnergyImportT2Kwh { get; set; }
        [JsonPropertyName("energy_import_t3_kwh")] public double? EnergyImportT3Kwh { get; set; }
        [JsonPropertyName("energy_import_t4_kwh")] public double? EnergyImportT4Kwh { get; set; }
        [JsonPropertyName("energy_export_kwh")] public double? EnergyExportKwh { get; set; }
        [JsonPropertyName("energy_export_t1_kwh")] public double? EnergyExportT1Kwh { get; set; }
        [JsonPropertyName("energy_export_t2_kwh")] public double? EnergyExportT2Kwh { get; set; }
        [JsonPropertyName("energy_export_t3_kwh")] public double? EnergyExportT3Kwh { get; set; }
        [JsonPropertyName("energy_export_t4_kwh")] public double? EnergyExportT4Kwh { get; set; }
        [JsonPropertyName("power_w")] public double? PowerW { get; set; }
        [JsonPropertyName("power_l1_w")] public double? PowerL1W { get; set; }
        [JsonPropertyName("power_l2_w")] public double? PowerL2W { get; set; }
        [JsonPropertyName("power_l3_w")] public double? PowerL3W { get; set; }
        [JsonPropertyName("voltage_v")] public double? VoltageV { get; set; }
        [JsonPropertyName("voltage_l1_v")] public double? VoltageL1V { get; set; }
        [JsonPropertyName("voltage_l2_v")] public double? VoltageL2V { get; set; }
        [JsonPropertyName("voltage_l3_v")] public double? VoltageL3V { get; set; }
        [JsonPropertyName("current_a")] public double? CurrentA { get; set; }
        [JsonPropertyName("current_l1_a")] public double? CurrentL1A { get; set; }
        [JsonPropertyName("current_l2_a")] public double? CurrentL2A { get; set; }
        [JsonPropertyName("current_l3_a")] public double? CurrentL3A { get; set; }
        [JsonPropertyName("frequency_hz")] public double? FrequencyHz { get; set; }
        [JsonPropertyName("voltage_sag_l1_count")] public double? VoltageSagL1Count { get; set; }
        [JsonPropertyName("voltage_sag_l2_count")] public double? VoltageSagL2Count { get; set; }
        [JsonPropertyName("voltage_sag_l3_count")] public double? VoltageSagL3Count { get; set; }
        [JsonPropertyName("voltage_swell_l1_count")] public double? VoltageSwellL1Count { get; set; }
        [JsonPropertyName("voltage_swell_l2_count")] public double? VoltageSwellL2Count { get; set; }
        [JsonPropertyName("voltage_swell_l3_count")] public double? VoltageSwellL3Count { get; set; }
        [JsonPropertyName("any_power_fail_count")] public double? AnyPowerFailCount { get; set; }
        [JsonPropertyName("long_power_fail_count")] public double? LongPowerFailCount { get; set; }
        [JsonPropertyName("average_power_15m_w")] public double? AveragePower15mW { get; set; }
        [JsonPropertyName("monthly_power_peak_w")] public double? MonthlyPowerPeakW { get; set; }
        [JsonPropertyName("monthly_power_peak_timestamp")] public DateTime? MonthlyPowerPeakTimestamp { get; set; }
    }
}
