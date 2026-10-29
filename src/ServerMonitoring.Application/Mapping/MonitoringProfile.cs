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
    }
}
