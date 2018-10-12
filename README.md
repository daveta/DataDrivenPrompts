This sample demonstrates a the use of prompt validations with ASP.Net Core 2.
 # To try this sample
- Clone the samples repository
```bash
git clone https://github.com/Microsoft/botbuilder-samples.git
```
- [Optional] Update the `appsettings.json` file under `botbuilder-Samples\samples\csharp_dotnetcore\10.prompt-validations` with your botFileSecret.  For Azure Bot Service bots, you can find the botFileSecret under application settings.
# Prerequisites

## Configure required services
1. Follow instructions [here](https://portal.azure.com) to create an Azure account. If you already have an account, sign in. Click on all services -> search for 'subscriptions' -> copy the subscription ID you would like to use from the Home > Subscriptions page.
2. Follow instructions [here](https://www.luis.ai/home) to create a LUIS.ai account. If you already have an account, sign in. Click on your name on top right corner of the screen -> settings and grab your authoring key.
3. To create and configure required LUIS and QnA Maker services, 
    - In a terminal,
        ```bash
        cd ./datadrivenprompt
        ```
    - Run MSbot Clone and pass in your LUIS authoring key and Azure subscription ID. This command will create required services for your bot and update the .bot file.
        ```bash
        msbot clone -n <YOUR-BOT-NAME> -f deploymentScripts/msbotClone -l <Bot service location> --luisAuthoringKey <Key from step-2 above> --subscriptionId <Key from step-1 above>
        ```
		The secret used to decrypt dd66.bot is:

NOTE: This secret is not recoverable and you should store this secret in a secure place according to best security practices.
Your project may be configured to rely on this secret and you should update it as appropriate.
dd66.bot created.
Done cloning.

## Visual Studio
- Navigate to the samples folder (`botbuilder-Samples\samples\csharp_dotnetcore\10.prompt-validations`) and open PromptValidationsBot.csproj in Visual Studio.
- Hit F5.
## Visual Studio Code
- Open `BotBuilder-Samples\samples\csharp_dotnetcore\04.simple-prompt` sample folder.
- Bring up a terminal, navigate to `botbuilder-Samples\samples\csharp_dotnetcore\10.prompt-validations` folder.
- Type 'dotnet run'.
## Update packages
- In Visual Studio right click on the solution and select "Restore NuGet Packages".
  **Note:** this sample requires `Microsoft.Bot.Builder.Dialogs` and `Microsoft.Bot.Builder.Integration.AspNet.Core`.
## Testing the bot using Bot Framework Emulator
[Microsoft Bot Framework Emulator](https://github.com/microsoft/botframework-emulator) is a desktop application that allows bot 
developers to test and debug their bots on localhost or running remotely through a tunnel.
- Install the Bot Framework emulator from [here](https://aka.ms/botframeworkemulator).
## Connect to bot using Bot Framework Emulator V4
- Launch the Bot Framework Emulator.
- File -> Open bot and navigate to `BotBuilder-Samples\samples\csharp_dotnetcore\10.prompt-validations` folder.
- Select `BotConfiguration.bot` file.
 # Further reading
- [Azure Bot Service](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-overview-introduction?view=azure-bot-service-4.0)
- [Bot Storage](https://docs.microsoft.com/en-us/azure/bot-service/dotnet/bot-builder-dotnet-state?view=azure-bot-service-3.0&viewFallbackFrom=azure-bot-service-4.0)
