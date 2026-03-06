using AutoMapper;
using SalonPro.Application.Features.Appointments.DTOs;
using SalonPro.Application.Features.Auth.DTOs;
using SalonPro.Application.Features.Clients.DTOs;
using SalonPro.Application.Features.Services.DTOs;
using SalonPro.Application.Features.Staff.DTOs;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;

namespace SalonPro.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Auth
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FirstName + " " + src.LastName));

        // Client (FullName, IsVip, Tags now on entity)
        CreateMap<Client, ClientDto>();

        CreateMap<Client, ClientListDto>()
            .ForMember(dest => dest.LastVisitDate, opt => opt.MapFrom(src =>
                src.Appointments
                    .Where(a => a.Status == AppointmentStatus.Completed)
                    .OrderByDescending(a => a.StartTime)
                    .Select(a => (DateTime?)a.StartTime)
                    .FirstOrDefault()))
            .ForMember(dest => dest.FavoriteService, opt => opt.MapFrom(src =>
                src.Appointments
                    .Where(a => a.Status == AppointmentStatus.Completed)
                    .SelectMany(a => a.AppointmentServices)
                    .GroupBy(aps => aps.Service.Name)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault()));

        CreateMap<ClientNote, ClientNoteDto>();

        CreateMap<Client, ClientDetailDto>()
            .ForMember(dest => dest.TotalVisits, opt => opt.MapFrom(src =>
                src.Appointments.Count(a => a.Status == AppointmentStatus.Completed)))
            .ForMember(dest => dest.TotalSpent, opt => opt.MapFrom(src =>
                src.Appointments
                    .Where(a => a.Status == AppointmentStatus.Completed)
                    .Sum(a => a.TotalPrice)))
            .ForMember(dest => dest.LastVisitDate, opt => opt.MapFrom(src =>
                src.Appointments
                    .Where(a => a.Status == AppointmentStatus.Completed)
                    .OrderByDescending(a => a.StartTime)
                    .Select(a => (DateTime?)a.StartTime)
                    .FirstOrDefault()))
            .ForMember(dest => dest.VisitHistory, opt => opt.MapFrom(src =>
                src.Appointments
                    .Where(a => a.Status == AppointmentStatus.Completed)
                    .OrderByDescending(a => a.StartTime)
                    .Select(a => new VisitHistoryDto(
                        a.StartTime,
                        string.Join(", ", a.AppointmentServices.Select(aps => aps.Service.Name)),
                        a.StaffMember.FullName,
                        a.TotalPrice))))
            .ForMember(dest => dest.ClientNotes, opt => opt.MapFrom(src =>
                src.ClientNotes
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new ClientNoteDto(n.Id, n.Content, n.CreatedAt, n.CreatedBy))));

        // Appointment list (for calendar/list views)
        CreateMap<Appointment, AppointmentListDto>()
            .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => src.Client.FullName))
            .ForMember(dest => dest.StaffMemberName, opt => opt.MapFrom(src => src.StaffMember.FullName))
            .ForMember(dest => dest.ServiceNames, opt => opt.MapFrom(src =>
                string.Join(", ", src.AppointmentServices.Select(aps => aps.Service.Name))))
            .ForMember(dest => dest.CategoryColorHex, opt => opt.MapFrom(src =>
                src.AppointmentServices.FirstOrDefault() != null
                    ? src.AppointmentServices.First().Service.Category.ColorHex
                    : (string?)null));

        // Appointment detail
        CreateMap<Appointment, AppointmentDto>()
            .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => src.Client.FullName))
            .ForMember(dest => dest.StaffMemberName, opt => opt.MapFrom(src => src.StaffMember.FullName))
            .ForMember(dest => dest.Services, opt => opt.MapFrom(src => src.AppointmentServices));

        CreateMap<AppointmentService, AppointmentServiceDetailDto>()
            .ForMember(dest => dest.ServiceName, opt => opt.MapFrom(src => src.Service.Name));

        CreateMap<Appointment, AppointmentDetailDto>()
            .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => src.Client.FullName))
            .ForMember(dest => dest.StaffMemberName, opt => opt.MapFrom(src => src.StaffMember.FullName))
            .ForMember(dest => dest.Services, opt => opt.MapFrom(src => src.AppointmentServices));

        // Service
        CreateMap<Service, ServiceDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
            .ForMember(dest => dest.CategoryType, opt => opt.MapFrom(src => src.Category.Type))
            .ForMember(dest => dest.CategoryColorHex, opt => opt.MapFrom(src => src.Category.ColorHex));

        // StaffMember
        CreateMap<StaffMember, StaffMemberDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FirstName + " " + src.LastName))
            .ForMember(dest => dest.AppointmentCountToday, opt => opt.Ignore());

        CreateMap<WorkingHours, WorkingHoursDto>();
    }
}
