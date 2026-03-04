using AutoMapper;
using SalonPro.Application.Features.Appointments.DTOs;
using SalonPro.Application.Features.Auth.DTOs;
using SalonPro.Application.Features.Clients.DTOs;
using SalonPro.Application.Features.Services.DTOs;
using SalonPro.Application.Features.ServiceCategories.DTOs;
using SalonPro.Application.Features.Staff.DTOs;
using SalonPro.Application.Features.WorkingHours.DTOs;
using SalonPro.Domain.Entities;

namespace SalonPro.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Auth
        CreateMap<User, AuthResponseDto>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .ForMember(d => d.AccessToken, opt => opt.Ignore())
            .ForMember(d => d.RefreshToken, opt => opt.Ignore())
            .ForMember(d => d.ExpiresAt, opt => opt.Ignore());

        // Clients
        CreateMap<Client, ClientDto>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"));
        CreateMap<CreateClientCommand, Client>();

        // Services
        CreateMap<Service, ServiceDto>();
        CreateMap<CreateServiceCommand, Service>();
        CreateMap<UpdateServiceCommand, Service>();

        // Service Categories
        CreateMap<ServiceCategory, ServiceCategoryDto>();
        CreateMap<CreateServiceCategoryCommand, ServiceCategory>();
        CreateMap<UpdateServiceCategoryCommand, ServiceCategory>();

        // Staff
        CreateMap<StaffMember, StaffMemberDto>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"));
        CreateMap<CreateStaffMemberCommand, StaffMember>();
        CreateMap<UpdateStaffMemberCommand, StaffMember>();

        // Appointments
        CreateMap<Appointment, AppointmentDto>()
            .ForMember(d => d.ClientName, opt => opt.MapFrom(s => s.Client != null ? $"{s.Client.FirstName} {s.Client.LastName}" : string.Empty))
            .ForMember(d => d.StaffMemberName, opt => opt.MapFrom(s => s.StaffMember != null ? $"{s.StaffMember.FirstName} {s.StaffMember.LastName}" : string.Empty))
            .ForMember(d => d.Services, opt => opt.MapFrom(s => s.AppointmentServices));
        CreateMap<AppointmentService, AppointmentServiceDto>()
            .ForMember(d => d.ServiceName, opt => opt.MapFrom(s => s.Service != null ? s.Service.Name : string.Empty));

        // Working Hours
        CreateMap<WorkingHours, WorkingHoursDto>();
        CreateMap<UpsertWorkingHoursCommand, WorkingHours>();
    }
}

// Command stubs to satisfy AutoMapper (real commands defined in Features)
public record CreateClientCommand;
public record CreateServiceCommand;
public record UpdateServiceCommand;
public record CreateServiceCategoryCommand;
public record UpdateServiceCategoryCommand;
public record CreateStaffMemberCommand;
public record UpdateStaffMemberCommand;
public record UpsertWorkingHoursCommand;
