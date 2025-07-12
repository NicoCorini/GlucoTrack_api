using System;
using System.Text.Json;
using System.Threading.Tasks;
using GlucoTrack_api.Data;
using GlucoTrack_api.Models;

namespace GlucoTrack_api.Utils
{
    /// <summary>
    /// Centralized service for logging changes to database entities.
    /// Tracks insert, update, and delete operations by saving the data before and after the operation in JSON format.
    /// </summary>
    public class ChangeLogService
    {
        private readonly GlucoTrackDBContext _context;

        public ChangeLogService(GlucoTrackDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Logs a change in the ChangeLogs table.
        /// </summary>
        /// <param name="doctorId">ID of the doctor performing the operation.</param>
        /// <param name="tableName">Name of the affected table.</param>
        /// <param name="recordId">ID of the modified record.</param>
        /// <param name="action">Type of action: Insert, Update, or Delete.</param>
        /// <param name="before">Object representing the state before the operation (can be null).</param>
        /// <param name="after">Object representing the state after the operation (can be null).</param>
        public async Task LogChangeAsync(int doctorId, string tableName, int recordId, string action, object? before, object? after)
        {
            var log = new ChangeLogs
            {
                DoctorId = doctorId,
                TableName = tableName,
                RecordId = recordId,
                Action = action,
                Timestamp = DateTime.UtcNow,
                DetailsBefore = before != null ? JsonSerializer.Serialize(before) : null,
                DetailsAfter = after != null ? JsonSerializer.Serialize(after) : null
            };

            _context.ChangeLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
