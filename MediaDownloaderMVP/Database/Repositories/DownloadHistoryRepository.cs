using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaDownloaderTgBotMVP.Database.Repositories
{
    public class DownloadHistoryRepository
    {
        private readonly AppDbContext _db;

        public DownloadHistoryRepository(AppDbContext db)
        {
            _db = db;
        }

    
    }
}
