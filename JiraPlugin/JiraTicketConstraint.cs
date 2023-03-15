using Rdmp.Core.Validation;
using Rdmp.Core.Validation.Constraints.Primary;

namespace JiraPlugin;

public class JiraTicketConstraint:PrimaryConstraint
{
    public override void RenameColumn(string originalName, string newName)
    {
            
    }

    public override string GetHumanReadableDescriptionOfValidation()
    {
        return "Jira Ticket";
    }

    public override ValidationFailure Validate(object value)
    {
        if (value == null)
            return null;
            
        var str = value as string ?? value.ToString();

        if (JIRATicketingSystem.RegexForTickets.IsMatch(str))
            return null;

        return new ValidationFailure("Value did not match jira ticket regex",this);
    }
}