using Privatekonomi.Core.Data;

namespace Privatekonomi.Core.Services.DataMigration;

public interface IDataMigrationService
{
    Task<DataMigrationPreview> PreviewAsync(DataMigrationRequest request, CancellationToken cancellationToken = default);
    Task<DataMigrationResult> MigrateAsync(DataMigrationRequest request, CancellationToken cancellationToken = default);
}
