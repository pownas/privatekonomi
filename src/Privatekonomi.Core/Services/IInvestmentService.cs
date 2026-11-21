using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

public interface IInvestmentService
{
    Task<IEnumerable<Investment>> GetAllInvestmentsAsync();
    Task<Investment?> GetInvestmentByIdAsync(int id);
    Task<Investment> AddInvestmentAsync(Investment investment);
    Task<Investment> UpdateInvestmentAsync(Investment investment);
    Task DeleteInvestmentAsync(int id);
    
    // Import and export
    Task<CsvImportResult> ImportFromCsvAsync(Stream csvStream, int bankSourceId);
    Task<string> ExportToCsvAsync(IEnumerable<Investment> investments);
    
    // Filtering
    Task<IEnumerable<Investment>> GetInvestmentsByBankAsync(int bankSourceId);
    Task<IEnumerable<Investment>> GetInvestmentsByAccountAsync(string accountNumber);

    Task<IEnumerable<InvestmentAccount>> GetInvestmentAccountsAsync();
    Task<InvestmentAccount?> GetInvestmentAccountByIdAsync(int id);
    Task<InvestmentAccount> AddInvestmentAccountAsync(InvestmentAccount account);
    Task<InvestmentAccount> UpdateInvestmentAccountAsync(InvestmentAccount account);
    Task DeleteInvestmentAccountAsync(int id);
}
