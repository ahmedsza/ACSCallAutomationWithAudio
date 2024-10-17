using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Communication.Identity;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc.Formatters;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Your ACS resource connection string
string acsConnectionString = "ACSCONNECTIONSTRING";



// Your ACS resource phone number will act as source number to start outbound call
var acsPhonenumber = "PUTINACSPHONENUMBER";

// Target phone number you want to receive the call.
var targetPhonenumber = "PUTINNUMBERTOCALL";

// Base url of the app
var callbackUriHost = "CALLBACKURI_CANUSEDEVTUNNEL";


CallAutomationClient callAutomationClient = new CallAutomationClient(acsConnectionString);
var app = builder.Build();

app.MapPost("/outboundCall", async (ILogger<Program> logger) =>
{
    
    PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhonenumber);
    PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhonenumber);

    CallInvite callInvite = new CallInvite(
        new PhoneNumberIdentifier(targetPhonenumber),
        new PhoneNumberIdentifier(acsPhonenumber)
    );  


    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    var createCallResult = await callAutomationClient.CreateCallAsync(
        callInvite,
        callbackUri
    );
    logger.LogInformation(createCallResult.Value.CallConnectionProperties.CallConnectionState.ToString());
    
});



app.MapPost("/api/callbacks", async (CloudEvent[] cloudEvents, ILogger<Program> logger) =>
{
    foreach (var cloudEvent in cloudEvents)
    {
        logger.LogInformation("Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}",
            cloudEvent.Type,
            cloudEvent.Subject,
            cloudEvent.Id);
        CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
        logger.LogInformation(
                    "Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}",
                    parsedEvent.GetType(),
                    parsedEvent.CallConnectionId,
                    parsedEvent.ServerCallId);

        var callConnection = callAutomationClient.GetCallConnection(parsedEvent.CallConnectionId);
        var callMedia = callConnection.GetCallMedia();
        if (parsedEvent is CallConnected callConnected)
            {
        logger.LogInformation("in call connected...");

        }
        string mp3Url = "https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav";
        var playSource = new FileSource(new Uri(mp3Url));
        PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhonenumber);
        var playTo = new List<CommunicationIdentifier> { target };
        var playResponse = await callAutomationClient.GetCallConnection(parsedEvent.CallConnectionId)
            .GetCallMedia()
            .PlayAsync(playSource, playTo);

      
        return Results.Ok();
    }
    return Results.Ok();
}).Produces(StatusCodes.Status200OK);


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



app.Run();
