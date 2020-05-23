# AgileObjects.Functions.Email

An Azure Function to send an email to a configured recipient.

## Setup

1. Create an [Azure Portal account](https://portal.azure.com).
2. Fork this repository.
3. [Create an Azure Function](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-azure-function)
4. [optional] [Set up your function to deploy from GitHub](https://docs.microsoft.com/en-us/azure/azure-functions/scripts/functions-cli-create-function-app-github-continuous). 
   Point it to your fork of this repository.
5. Set up the following [App Settings for your Azure Function App](https://docs.microsoft.com/en-us/azure/azure-functions/functions-how-to-use-azure-function-app-settings).

| Setting           | Value |
|-------------------|-------|
| SmtpHost          | The URL of the email server to use to send the emails. |
| SmtpUsername      | The username of the account to use to send the emails. |
| SmtpPassword      | The password of the account to use to send the emails. |
| Recipient         | The email address to which the function will send emails. |
| IsSubjectRequired | [optional] A boolean value indicating whether the posted form data must contain an email subject. |
| FallbackSubject   | [optional] The subject for the email to send, if one is optional and not supplied in posted form data. |

## Use

Use an HTML form or AJAX call to post the following data to the function URL:

| Name    | Value |
|---------|-------|
| name    | The sender's name. |
| email   | The sender's email address. |
| subject | [optional] The subject of the sender's email. If no value is supplied and a subject is optional, the `FallbackSubject` setting will be used to provide the email subject. |
| message | The sender's message. |

## Responses

The function will respond with one of the following:

| Status | Reason |
|--------|--------|
| 200    | Email sent successfully. |
| 500    | Something unexpected went wrong. |
| 400    | A piece of required information was either missing, or invalid. A collection of new-line-separated error messages is returned to say what. |