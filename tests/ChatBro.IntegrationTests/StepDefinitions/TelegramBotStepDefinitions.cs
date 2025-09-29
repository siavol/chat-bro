namespace ChatBro.IntegrationTests.StepDefinitions;

[Binding]
public class TelegramBotStepDefinitions
{
    [When("telegram user sends a message {string}")]
    public void WhenTelegramUserSendsAMessage(string p0)
    {
        throw new PendingStepException();
    }

    [Then("bot responds in telegram with {string}")]
    public void ThenBotRespondsInTelegramWith(string p0)
    {
        throw new PendingStepException();
    }
}