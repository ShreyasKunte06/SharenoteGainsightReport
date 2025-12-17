using Microsoft.Extensions.DependencyInjection;
using SharenoteGainsight.DataAccess;
using SharenoteGainsight.DataAccess.Repository;
using SharenoteGainsight.Infrastructure.Csv;
using SharenoteGainsight.Infrastructure.Sftp;
using SharenoteGainsight.Services;


namespace SharenoteGainsight.Worker.DI
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddMyServices(this IServiceCollection services)
        {
            // DataAccess
            services.AddScoped<IStaffRepository, StaffRepository>();

            // Infrastructure
            services.AddScoped<ICsvExporter, CsvExporter>();
            services.AddScoped<ISftpService, SftpService>();

            // Services
            services.AddScoped<IStaffExportService, StaffExportService>();

            return services;
        }
    }
}
