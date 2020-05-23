# AgileObjects.Functions.Email

A .NET Core 3.1 Azure Function to send an email to a configured recipient.

## Setup

1. Create an [Azure Portal account](https://portal.azure.com).
2. Fork this repository.
3. [Create an Azure Function](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-azure-function)
4. [optional] [Set up your function to deploy from GitHub](https://docs.microsoft.com/en-us/azure/azure-functions/scripts/functions-cli-create-function-app-github-continuous). 
   Point it to your fork of this repository.
5. Set up the following [App Settings for your Azure Function App](https://docs.microsoft.com/en-us/azure/azure-functions/functions-how-to-use-azure-function-app-settings).

| Setting               | Value |
|-----------------------|-------|
| SmtpHost              | The URL of the email server to use to send the emails. |
| SmtpUsername          | The username of the account to use to send the emails. |
| SmtpPassword          | The password of the account to use to send the emails. |
| Recipient             | The email address to which the function will send emails. |
| IsSubjectRequired     | [optional] A boolean value indicating whether the posted form data must contain an email subject. Defaults to false. |
| FallbackSubject       | [optional] The subject for the email to send, if one is optional and not supplied in posted form data. Defaults to 'Email received'. |
| UseRedirectResponse   | [optional] A boolean value indicating whether the function should respond with a redirect (302) response or an OK (200) response. Default to false, yielding OK responses. |
| AllowUserRedirectUrls | [optional] A boolean value indicating whether the function should redirect to a posted `responseUrl`. Default to false, yielding redirects to the configured `SuccessRedirectUrl`. |
| SuccessRedirectUrl    | [optional] A fixed redirect URL with which to respond to a caller, if the function either disallows user-supplied redirect URLs, or no direct URL is supplied. |


## Use

Use an HTML form or AJAX call to post the following data to the function URL:

| Name        | Value |
|-------------|-------|
| name        | The sender's name. |
| email       | The sender's email address. |
| subject     | [optional] The subject of the sender's email. If no value is supplied and a subject is optional, the `FallbackSubject` setting will be used to provide the email subject. |
| message     | The sender's message. |
| redirectUrl | [optional] The redirect URL with which to respond to the caller, if a redirect response is desired and enabled. |

## Responses

The function will respond with one of the following:

| Status | Reason |
|--------|--------|
| 200    | Email sent successfully, when redirecting is not enabled. |
| 302    | Email sent successfully, when redirecting is enabled. |
| 500    | Something unexpected went wrong. |
| 400    | A piece of required information was either missing, or invalid. A collection of new-line-separated error messages is returned to say what. 

## Redirect Response Examples

#### Redirect responses disabled

- `UseRedirectResponse`: false
- `AllowUserRedirectUrls`: false
- `redirectUrl`: [not supplied]
- `SuccessRedirectUrl`: [not configured]
- _Response_: **OK** (200), as `UseRedirectResponse` is false.

#### User redirect response URL disallowed

- `UseRedirectResponse`: true
- `AllowUserRedirectUrls`: false
- `redirectUrl`: [not supplied]
- `SuccessRedirectUrl`: /contact-thank-you
- _Response_: **Redirect** (302) to /contact-thank-you, as `AllowUserRedirectUrls` is false.

#### User redirect response URL allowed and supplied

- `UseRedirectResponse`: true
- `AllowUserRedirectUrls`: true
- `redirectUrl`: /special-thank-you
- `SuccessRedirectUrl`: /contact-thank-you
- _Response_: **Redirect** (302) to /special-thank-you, as `AllowUserRedirectUrls` is true and a URL was supplied.

#### User redirect response URL allowed, but not supplied

- `UseRedirectResponse`: true
- `AllowUserRedirectUrls`: true
- `redirectUrl`: [not supplied]
- `SuccessRedirectUrl`: /contact-thank-you
- _Response_: **Redirect** (302) to /contact-thank-you, as `AllowUserRedirectUrls` is true, but no URL was supplied.

#### User redirect response URL allowed, not supplied, and required

- `UseRedirectResponse`: true
- `AllowUserRedirectUrls`: true
- `redirectUrl`: [not supplied]
- `SuccessRedirectUrl`: [not configured]
- _Response_: **Bad Request** (400), as no fallback `SuccessRedirectUrl` is configured, and no URL was supplied.