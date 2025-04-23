namespace TecChallenge.Data.Repositories;

public class LocalizationRecordRepository(AppDbContext context)
    : Repository<LocalizationRecord>(context),
        ILocalizationRecordRepository;