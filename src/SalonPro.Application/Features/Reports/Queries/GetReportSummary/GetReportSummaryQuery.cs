using MediatR;
using SalonPro.Application.Features.Reports.DTOs;

namespace SalonPro.Application.Features.Reports.Queries.GetReportSummary;

public record GetReportSummaryQuery(DateTime From, DateTime To) : IRequest<ReportSummaryDto>;
