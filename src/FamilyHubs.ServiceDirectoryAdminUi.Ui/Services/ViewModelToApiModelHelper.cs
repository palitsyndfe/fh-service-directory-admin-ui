using FamilyHubs.ServiceDirectory.Shared.Builders;
using FamilyHubs.ServiceDirectory.Shared.Enums;
using FamilyHubs.ServiceDirectory.Shared.Models.Api.OpenReferralContacts;
using FamilyHubs.ServiceDirectory.Shared.Models.Api.OpenReferralCostOptions;
using FamilyHubs.ServiceDirectory.Shared.Models.Api.OpenReferralEligibilitys;
using FamilyHubs.ServiceDirectory.Shared.Models.Api.OpenReferralLanguages;
using FamilyHubs.ServiceDirectory.Shared.Models.Api.OpenReferralLocations;
using FamilyHubs.ServiceDirectory.Shared.Models.Api.OpenReferralOrganisations;
using FamilyHubs.ServiceDirectory.Shared.Models.Api.OpenReferralPhones;
using FamilyHubs.ServiceDirectory.Shared.Models.Api.OpenReferralPhysicalAddresses;
using FamilyHubs.ServiceDirectory.Shared.Models.Api.OpenReferralServiceAreas;
using FamilyHubs.ServiceDirectory.Shared.Models.Api.OpenReferralServiceAtLocations;
using FamilyHubs.ServiceDirectory.Shared.Models.Api.OpenReferralServiceDeliverysEx;
using FamilyHubs.ServiceDirectory.Shared.Models.Api.OpenReferralServices;
using FamilyHubs.ServiceDirectory.Shared.Models.Api.OpenReferralServiceTaxonomys;
using FamilyHubs.ServiceDirectory.Shared.Models.Api.OpenReferralTaxonomys;
using FamilyHubs.ServiceDirectoryAdminUi.Ui.Models;
using FamilyHubs.ServiceDirectoryAdminUi.Ui.Services.Api;
using FamilyHubs.SharedKernel;

namespace FamilyHubs.ServiceDirectoryAdminUi.Ui.Services;

public interface IViewModelToApiModelHelper
{
    Task<IOpenReferralOrganisationWithServicesDto> GetOrganisation(OrganisationViewModel viewModel);
}

public class ViewModelToApiModelHelper : IViewModelToApiModelHelper
{
    private readonly IOpenReferralOrganisationAdminClientService _openReferralOrganisationAdminClientService;
    public ViewModelToApiModelHelper(IOpenReferralOrganisationAdminClientService openReferralOrganisationAdminClientService)
    {
        _openReferralOrganisationAdminClientService = openReferralOrganisationAdminClientService;
    }

    //----------------------------------------------------------
    // **** TODO: Refactor to use the Builder Pattern
    // **** https://www.dofactory.com/net/builder-design-pattern
    //----------------------------------------------------------
    public async Task<IOpenReferralOrganisationWithServicesDto> GetOrganisation(OrganisationViewModel viewModel)
    {
        OrganisationDtoBuilder builder = new();
        IOpenReferralOrganisationWithServicesDto organisation = builder.WithMainProperties(
            viewModel.Id.ToString(),
            viewModel.Name,
            viewModel.Description,
            viewModel.Logo,
            new Uri(viewModel.Url ?? string.Empty).ToString(),
            viewModel.Url
            )
            .WithServices(
            new List<IOpenReferralServiceDto>
            {
                await GetService(viewModel)
            }
            )
            .Build();

        return organisation;
    }

    private async Task<IOpenReferralServiceDto> GetService(OrganisationViewModel viewModel)
    {
        var contactId = Guid.NewGuid().ToString();

        ServicesDtoBuilder builder = new ServicesDtoBuilder();
        IOpenReferralServiceDto service = builder.WithMainProperties(viewModel.ServiceId ?? Guid.NewGuid().ToString(),
                viewModel.ServiceName ?? string.Empty,
                viewModel.ServiceDescription,
                null,
                null,
                null,
                null,
                string.Join(",", viewModel.InPersonSelection != null ? viewModel.InPersonSelection.ToArray() : Array.Empty<string>()),
                "pending",
                viewModel.Website,
                viewModel.Email,
                null)
            .WithServiceDelivery(GetDeliveryTypes(viewModel.ServiceDeliverySelection))
            .WithEligibility(GetEligibilities("Children", viewModel.MinAge ?? 0, viewModel.MaxAge ?? 0))
            .WithContact(new List<IOpenReferralContactDto>()
            {
                new OpenReferralContactDto(
                    contactId,
                    "Service",
                    string.Empty,
                    new List<IOpenReferralPhoneDto>()
                    {
                        new OpenReferralPhoneDto(contactId, viewModel.Telephone ?? string.Empty)
                    }
                    )
            })
            .WithCostOption(GetCost(viewModel.IsPayedFor == "Yes", viewModel.PayUnit ?? string.Empty, viewModel.Cost))
            .WithLanguages(GetLanguages(viewModel.Languages))
            .WithServiceAreas(new List<IOpenReferralServiceAreaDto>()
            {
                new OpenReferralServiceAreaDto(Guid.NewGuid().ToString(), "Local", null, "http://statistics.data.gov.uk/id/statistical-geography/K02000001")

            })
            .WithServiceAtLocations(GetServiceAtLocation(viewModel))
            .WithServiceTaxonomies(await GetOpenReferralTaxonomies(viewModel?.TaxonomySelection))
            .Build();

        return service;
    }

    private static List<IOpenReferralServiceAtLocationDto> GetServiceAtLocation(OrganisationViewModel viewModel)
    {
        

        List<IOpenReferralPhysicalAddressDto> address = new()
        {
            new OpenReferralPhysicalAddressDto(
                Guid.NewGuid().ToString(),
                viewModel?.Address_1 ?? string.Empty,
                viewModel?.City ?? string.Empty,
                viewModel?.Postal_code ?? string.Empty,
                "England",
                viewModel?.State_province ?? string.Empty)
        };

        IOpenReferralLocationDto location = new OpenReferralLocationDto(
            Guid.NewGuid().ToString(),
            "Our Location",
            "",
            viewModel?.Latitude ?? 0.0D,
            viewModel?.Longtitude ?? 0.0D,
            address);

        List<IOpenReferralServiceAtLocationDto> list = new()
        {
            new OpenReferralServiceAtLocationDto(Guid.NewGuid().ToString(), location)
        };

        return list;
    }

    private static List<IOpenReferralCostOptionDto> GetCost(bool isPayedFor, string payUnit, decimal? cost)
    {
        List<IOpenReferralCostOptionDto> list = new();

        if (isPayedFor && cost != null)
        {
            list.Add(new OpenReferralCostOptionDto(Guid.NewGuid().ToString(), payUnit ?? string.Empty, cost.Value, null, null, null, null));
        }

        return list;
    }

    private static List<IOpenReferralServiceDeliveryExDto> GetDeliveryTypes(List<string>? serviceDeliverySelection)
    {
        List<IOpenReferralServiceDeliveryExDto> list = new();
        if (serviceDeliverySelection == null)
            return list;

        foreach (var serviceDelivery in serviceDeliverySelection)
        {
            switch (serviceDelivery)
            {

                case "1":
                    list.Add(new OpenReferralServiceDeliveryExDto(Guid.NewGuid().ToString(), ServiceDelivery.InPerson));
                    break;
                case "2":
                    list.Add(new OpenReferralServiceDeliveryExDto(Guid.NewGuid().ToString(), ServiceDelivery.Online));
                    break;
                case "3":
                    list.Add(new OpenReferralServiceDeliveryExDto(Guid.NewGuid().ToString(), ServiceDelivery.Telephone));
                    break;
            }
        }

        return list;
    }

    private static List<IOpenReferralEligibilityDto> GetEligibilities(string whoFor, int minAge, int maxAge)
    {
        List<IOpenReferralEligibilityDto> list = new()
        {
            new OpenReferralEligibilityDto(Guid.NewGuid().ToString(), whoFor, maxAge, minAge)
        };

        return list;
    }

    private async Task<List<IOpenReferralServiceTaxonomyDto>> GetOpenReferralTaxonomies(List<string>? taxonomySelection)
    {
        List<IOpenReferralServiceTaxonomyDto> OpenReferralTaxonomyDtos = new();

        PaginatedList<IOpenReferralTaxonomyDto> taxonomies = (PaginatedList<IOpenReferralTaxonomyDto>)(IOpenReferralTaxonomyDto) await _openReferralOrganisationAdminClientService.GetTaxonomyList(1, 9999);

        if (taxonomies != null && taxonomySelection != null)
        {
            foreach (string taxonomyKey in taxonomySelection)
            {
                IOpenReferralTaxonomyDto? taxonomy = taxonomies.Items.FirstOrDefault(x => x.Id == taxonomyKey);
                if (taxonomy != null)
                {
                    OpenReferralTaxonomyDtos.Add(new OpenReferralServiceTaxonomyDto(Guid.NewGuid().ToString(), taxonomy));
                }
            }
        }

        return OpenReferralTaxonomyDtos;
    }

    private static List<IOpenReferralLanguageDto> GetLanguages(List<string>? viewModellanguages)
    {
        List<IOpenReferralLanguageDto> languages = new();

        if (viewModellanguages != null)
        {
            foreach (string lang in viewModellanguages)
            {
                languages.Add(new OpenReferralLanguageDto(Guid.NewGuid().ToString(), lang));
            }
        }

        return languages;
    }
}
