using LeadStatusUpdater.Core.Enums;

namespace LeadStatusUpdater.Core.DTOs;

public class LeadDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Mail { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public DateTime BirthDate { get; set; }
    public LeadStatus Status { get; set; }
    public List<AccountDto> Accounts { get; set; }
}