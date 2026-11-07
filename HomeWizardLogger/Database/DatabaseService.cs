using HomeWizardLogger.Configurations;
using HomeWizardLogger.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace HomeWizardLogger.Database;

public interface IDatabaseService
{
    Task<bool> InsertMeasurementAsync(Measurement measurement, CancellationToken cancellationToken = default);
}

public class DatabaseService(ILogger<DatabaseService> logger, IOptions<TimescaleDbSettings> timescaleDbSettings)
    : IDatabaseService
{
    private readonly TimescaleDbSettings _timescaleDbSettings = timescaleDbSettings.Value;

    public async Task<bool> InsertMeasurementAsync(Measurement measurement, CancellationToken cancellationToken = default)
    {
        if (!_timescaleDbSettings.Enabled)
            return false;
        try
        {
            var timestamp = measurement.TimestampUtc
                ?? throw new InvalidOperationException("Measurement timestamp must be resolved at receipt before inserting.");
            await using var conn = new NpgsqlConnection(_timescaleDbSettings.ConnectionString);
            await conn.OpenAsync(cancellationToken);

        var sql = @"
            INSERT INTO homewizard_measurements (
                unique_id, protocol_version, meter_model, timestamp, tariff,
                energy_import_kwh, energy_import_t1_kwh, energy_import_t2_kwh, energy_import_t3_kwh, energy_import_t4_kwh,
                energy_export_kwh, energy_export_t1_kwh, energy_export_t2_kwh, energy_export_t3_kwh, energy_export_t4_kwh,
                power_w, power_l1_w, power_l2_w, power_l3_w,
                voltage_v, voltage_l1_v, voltage_l2_v, voltage_l3_v,
                current_a, current_l1_a, current_l2_a, current_l3_a,
                frequency_hz,
                voltage_sag_l1_count, voltage_sag_l2_count, voltage_sag_l3_count,
                voltage_swell_l1_count, voltage_swell_l2_count, voltage_swell_l3_count,
                any_power_fail_count, long_power_fail_count,
                average_power_15m_w, monthly_power_peak_w, monthly_power_peak_timestamp
            ) VALUES (
                @unique_id, @protocol_version, @meter_model, @timestamp, @tariff,
                @energy_import_kwh, @energy_import_t1_kwh, @energy_import_t2_kwh, @energy_import_t3_kwh, @energy_import_t4_kwh,
                @energy_export_kwh, @energy_export_t1_kwh, @energy_export_t2_kwh, @energy_export_t3_kwh, @energy_export_t4_kwh,
                @power_w, @power_l1_w, @power_l2_w, @power_l3_w,
                @voltage_v, @voltage_l1_v, @voltage_l2_v, @voltage_l3_v,
                @current_a, @current_l1_a, @current_l2_a, @current_l3_a,
                @frequency_hz,
                @voltage_sag_l1_count, @voltage_sag_l2_count, @voltage_sag_l3_count,
                @voltage_swell_l1_count, @voltage_swell_l2_count, @voltage_swell_l3_count,
                @any_power_fail_count, @long_power_fail_count,
                @average_power_15m_w, @monthly_power_peak_w, @monthly_power_peak_timestamp
            )
            ON CONFLICT (timestamp, unique_id) DO UPDATE SET
                protocol_version = EXCLUDED.protocol_version,
                meter_model = EXCLUDED.meter_model,
                tariff = EXCLUDED.tariff,
                energy_import_kwh = EXCLUDED.energy_import_kwh,
                energy_import_t1_kwh = EXCLUDED.energy_import_t1_kwh,
                energy_import_t2_kwh = EXCLUDED.energy_import_t2_kwh,
                energy_import_t3_kwh = EXCLUDED.energy_import_t3_kwh,
                energy_import_t4_kwh = EXCLUDED.energy_import_t4_kwh,
                energy_export_kwh = EXCLUDED.energy_export_kwh,
                energy_export_t1_kwh = EXCLUDED.energy_export_t1_kwh,
                energy_export_t2_kwh = EXCLUDED.energy_export_t2_kwh,
                energy_export_t3_kwh = EXCLUDED.energy_export_t3_kwh,
                energy_export_t4_kwh = EXCLUDED.energy_export_t4_kwh,
                power_w = EXCLUDED.power_w,
                power_l1_w = EXCLUDED.power_l1_w,
                power_l2_w = EXCLUDED.power_l2_w,
                power_l3_w = EXCLUDED.power_l3_w,
                voltage_v = EXCLUDED.voltage_v,
                voltage_l1_v = EXCLUDED.voltage_l1_v,
                voltage_l2_v = EXCLUDED.voltage_l2_v,
                voltage_l3_v = EXCLUDED.voltage_l3_v,
                current_a = EXCLUDED.current_a,
                current_l1_a = EXCLUDED.current_l1_a,
                current_l2_a = EXCLUDED.current_l2_a,
                current_l3_a = EXCLUDED.current_l3_a,
                frequency_hz = EXCLUDED.frequency_hz,
                voltage_sag_l1_count = EXCLUDED.voltage_sag_l1_count,
                voltage_sag_l2_count = EXCLUDED.voltage_sag_l2_count,
                voltage_sag_l3_count = EXCLUDED.voltage_sag_l3_count,
                voltage_swell_l1_count = EXCLUDED.voltage_swell_l1_count,
                voltage_swell_l2_count = EXCLUDED.voltage_swell_l2_count,
                voltage_swell_l3_count = EXCLUDED.voltage_swell_l3_count,
                any_power_fail_count = EXCLUDED.any_power_fail_count,
                long_power_fail_count = EXCLUDED.long_power_fail_count,
                average_power_15m_w = EXCLUDED.average_power_15m_w,
                monthly_power_peak_w = EXCLUDED.monthly_power_peak_w,
                monthly_power_peak_timestamp = EXCLUDED.monthly_power_peak_timestamp
        ";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("unique_id", (object?)measurement.UniqueId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("protocol_version", (object?)measurement.ProtocolVersion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("meter_model", (object?)measurement.MeterModel ?? DBNull.Value);
            cmd.Parameters.AddWithValue("timestamp", timestamp);
            cmd.Parameters.AddWithValue("tariff", (object?)measurement.Tariff ?? DBNull.Value);
            cmd.Parameters.AddWithValue("energy_import_kwh", (object?)measurement.EnergyImportKwh ?? DBNull.Value);
            cmd.Parameters.AddWithValue("energy_import_t1_kwh", (object?)measurement.EnergyImportT1Kwh ?? DBNull.Value);
            cmd.Parameters.AddWithValue("energy_import_t2_kwh", (object?)measurement.EnergyImportT2Kwh ?? DBNull.Value);
            cmd.Parameters.AddWithValue("energy_import_t3_kwh", (object?)measurement.EnergyImportT3Kwh ?? DBNull.Value);
            cmd.Parameters.AddWithValue("energy_import_t4_kwh", (object?)measurement.EnergyImportT4Kwh ?? DBNull.Value);
            cmd.Parameters.AddWithValue("energy_export_kwh", (object?)measurement.EnergyExportKwh ?? DBNull.Value);
            cmd.Parameters.AddWithValue("energy_export_t1_kwh", (object?)measurement.EnergyExportT1Kwh ?? DBNull.Value);
            cmd.Parameters.AddWithValue("energy_export_t2_kwh", (object?)measurement.EnergyExportT2Kwh ?? DBNull.Value);
            cmd.Parameters.AddWithValue("energy_export_t3_kwh", (object?)measurement.EnergyExportT3Kwh ?? DBNull.Value);
            cmd.Parameters.AddWithValue("energy_export_t4_kwh", (object?)measurement.EnergyExportT4Kwh ?? DBNull.Value);
            cmd.Parameters.AddWithValue("power_w", (object?)measurement.PowerW ?? DBNull.Value);
            cmd.Parameters.AddWithValue("power_l1_w", (object?)measurement.PowerL1W ?? DBNull.Value);
            cmd.Parameters.AddWithValue("power_l2_w", (object?)measurement.PowerL2W ?? DBNull.Value);
            cmd.Parameters.AddWithValue("power_l3_w", (object?)measurement.PowerL3W ?? DBNull.Value);
            cmd.Parameters.AddWithValue("voltage_v", (object?)measurement.VoltageV ?? DBNull.Value);
            cmd.Parameters.AddWithValue("voltage_l1_v", (object?)measurement.VoltageL1V ?? DBNull.Value);
            cmd.Parameters.AddWithValue("voltage_l2_v", (object?)measurement.VoltageL2V ?? DBNull.Value);
            cmd.Parameters.AddWithValue("voltage_l3_v", (object?)measurement.VoltageL3V ?? DBNull.Value);
            cmd.Parameters.AddWithValue("current_a", (object?)measurement.CurrentA ?? DBNull.Value);
            cmd.Parameters.AddWithValue("current_l1_a", (object?)measurement.CurrentL1A ?? DBNull.Value);
            cmd.Parameters.AddWithValue("current_l2_a", (object?)measurement.CurrentL2A ?? DBNull.Value);
            cmd.Parameters.AddWithValue("current_l3_a", (object?)measurement.CurrentL3A ?? DBNull.Value);
            cmd.Parameters.AddWithValue("frequency_hz", (object?)measurement.FrequencyHz ?? DBNull.Value);
            cmd.Parameters.AddWithValue("voltage_sag_l1_count", (object?)measurement.VoltageSagL1Count ?? DBNull.Value);
            cmd.Parameters.AddWithValue("voltage_sag_l2_count", (object?)measurement.VoltageSagL2Count ?? DBNull.Value);
            cmd.Parameters.AddWithValue("voltage_sag_l3_count", (object?)measurement.VoltageSagL3Count ?? DBNull.Value);
            cmd.Parameters.AddWithValue("voltage_swell_l1_count", (object?)measurement.VoltageSwellL1Count ?? DBNull.Value);
            cmd.Parameters.AddWithValue("voltage_swell_l2_count", (object?)measurement.VoltageSwellL2Count ?? DBNull.Value);
            cmd.Parameters.AddWithValue("voltage_swell_l3_count", (object?)measurement.VoltageSwellL3Count ?? DBNull.Value);
            cmd.Parameters.AddWithValue("any_power_fail_count", (object?)measurement.AnyPowerFailCount ?? DBNull.Value);
            cmd.Parameters.AddWithValue("long_power_fail_count", (object?)measurement.LongPowerFailCount ?? DBNull.Value);
            cmd.Parameters.AddWithValue("average_power_15m_w", (object?)measurement.AveragePower15mW ?? DBNull.Value);
            cmd.Parameters.AddWithValue("monthly_power_peak_w", (object?)measurement.MonthlyPowerPeakW ?? DBNull.Value);
            cmd.Parameters.AddWithValue("monthly_power_peak_timestamp", (object?)measurement.MonthlyPowerPeakTimestamp ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error inserting measurement into TimescaleDB");
            return false;
        }
    }
}