using Privatekonomi.Core.Data;

namespace Privatekonomi.Core.Services.DataMigration;

public interface IDataMigrationService
{
    Task<DataMigrationResult> MigrateAsync(DataMigrationRequest request, CancellationToken cancellationToken = default);
}
