using AutoMapper;
using ServerMonitoring.Application.Dtos;
using ServerMonitoring.Domain.Entities;

namespace ServerMonitoring.Application.Mapping;

public sealed class MonitoringProfile : Profile
{
    public MonitoringProfile()
    {
        CreateMap<NewRequestObservationDto, RequestTrace>()
            .ForMember(trace => trace.Id, options => options.Ignore());

        CreateMap<NewSystemSampleDto, SystemSample>()
            .ForMember(sample => sample.Id, options => options.Ignore());

        CreateMap<RequestTrace, RecentRequestDto>();
        CreateMap<SystemSample, SystemSampleDto>();

        CreateMap<NewServerSampleDto, ServerSample>()
            .ForMember(sample => sample.Id, options => options.Ignore())
            .ForMember(sample => sample.Disks, options => options.MapFrom(dto => dto.Disks))
            .ForMember(sample => sample.TopProcesses, options => options.MapFrom(dto => dto.TopProcesses));

        CreateMap<DiskDto, DiskSample>().ReverseMap()
            .ForMember(dto => dto.UsedPercent, options => options.MapFrom(sample =>
                sample.TotalBytes <= 0
                    ? 0
                    : Math.Round((sample.TotalBytes - sample.AvailableBytes) * 100d / sample.TotalBytes, 2)));

        CreateMap<ProcessUsageDto, ProcessUsageSample>().ReverseMap();

        CreateMap<ServerSample, ServerSampleDto>();

        CreateMap<NewDomainCheckDto, DomainCheck>()
            .ForMember(check => check.Id, options => options.Ignore());
    }
}

