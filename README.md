# AgileObjects.Functions.Email

A .NET Core 3.1 Azure Function to send an email to a configured recipient.

## Setup

1. Create an [Azure Portal account](https://portal.azure.com).
2. Fork this repository.
3. [Create an Azure Function](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-azure-function)
4. Set up your function to deploy [from GitHub](https://docs.microsoft.com/en-us/azure/azure-functions/scripts/functions-cli-create-function-app-github-continuous),
   pointed to your fork of this repository, or [publish it from Visual Studio](https://tutorials.visualstudio.com/first-azure-function/publish) 
   from your repository fork.


## Configuration

The Function can be configured to require a subject, and to respond with OK (200, for AJAX) or redirect 
(302, for form post) responses. OK responses will include a response body containing 
`{ Redirect: 'SuccessRedirectUrl' }` if `SuccessRedirectUrl` is configured.
The following App Settings are used, and should be 
[configured](https://docs.microsoft.com/en-us/azure/azure-functions/functions-how-to-use-azure-function-app-settings):

| Setting               | Value |
|-----------------------|-------|
| SmtpHost              | The URL of the email server to use to send the emails. |
| SmtpUsername          | The username of the account to use to send the emails. |
| SmtpPassword          | The password of the account to use to send the emails. |
| Recipient             | The email address to which the function will send emails. |
| IsSubjectRequired     | [optional] A boolean value indicating whether the posted form data must contain an email subject. Defaults to false. |
| FallbackSubject       | [optional] The subject for the email to send, if one is optional and not supplied in posted form data. Defaults to 'Email received'. |
| UseRedirectResponse   | [optional] A boolean value indicating whether the function should respond with a redirect (302) response or an OK (200) response. Default to false, yielding OK responses. |
| AllowUserRedirectUrls | [optional] A boolean value indicating whether the function should support a posted `redirectUrl`. Default to false, yielding redirects to the configured `SuccessRedirectUrl`. |
| SuccessRedirectUrl    | [optional] A fixed redirect URL with which to respond to a caller, if the function either disallows user-supplied redirect URLs, or no redirect URL is supplied. |


## Use

Use an AJAX call or HTML form to post the following data to the function URL:

| Name        | Value |
|-------------|-------|
| name        | The sender's name. |
| email       | The sender's email address. |
| subject     | [optional] The subject of the sender's email. If no value is supplied and a subject is optional, the `FallbackSubject` setting will be used to provide the email subject. |
| message     | The sender's message. |
| redirectUrl | [optional] The redirect URL with which to respond to the caller, if posted redirect URLs are supported. |

## Responses

Depending on configuration and posted data, the function will respond with one of the following:

| Status | Reason |
|--------|--------|
| 200    | Email sent successfully, when redirecting is not enabled. |
| 302    | Email sent successfully, when redirecting is enabled. |
| 400    | A piece of required information was either missing, or invalid. A collection of new-line-separated error messages is returned to say what. |
| 500    | Something unexpected went wrong. |

## Redirect Response Examples

For clarity, here's some examples of what the Function will return, based on how you configure it and 
the data it's given:

#### Redirect responses disabled, no SuccessRedirectUrl:

- `UseRedirectResponse`: false
- `AllowUserRedirectUrls`: false
- `redirectUrl`: [not supplied]
- `SuccessRedirectUrl`: [not configured]
- _Response_: **OK** (200), as `UseRedirectResponse` is false.

#### Redirect responses disabled, configured SuccessRedirectUrl:

- `UseRedirectResponse`: false
- `AllowUserRedirectUrls`: false
- `redirectUrl`: [not supplied]
- `SuccessRedirectUrl`: /contact-thank-you
- _Response_: **OK** (200) with response body `{ Redirect: '/contact-thank-you' }`, as 
  `UseRedirectResponse` is false, but `SuccessRedirectUrl` is configured.

#### User redirect response URL disallowed:

- `UseRedirectResponse`: true
- `AllowUserRedirectUrls`: false
- `redirectUrl`: [not supplied]
- `SuccessRedirectUrl`: /contact-thank-you
- _Response_: **Redirect** (302) to /contact-thank-you, as `AllowUserRedirectUrls` is false.

#### User redirect response URL allowed and supplied:

- `UseRedirectResponse`: true
- `AllowUserRedirectUrls`: true
- `redirectUrl`: /special-thank-you
- `SuccessRedirectUrl`: /contact-thank-you
- _Response_: **Redirect** (302) to /special-thank-you, as `AllowUserRedirectUrls` is true and a URL was supplied.

#### User redirect response URL allowed, but not supplied:

- `UseRedirectResponse`: true
- `AllowUserRedirectUrls`: true
- `redirectUrl`: [not supplied]
- `SuccessRedirectUrl`: /contact-thank-you
- _Response_: **Redirect** (302) to /contact-thank-you, as `AllowUserRedirectUrls` is true, but no URL was supplied.

#### User redirect response URL allowed, not supplied, and required:

- `UseRedirectResponse`: true
- `AllowUserRedirectUrls`: true
- `redirectUrl`: [not supplied]
- `SuccessRedirectUrl`: [not configured]
- _Response_: **Bad Request** (400), as no fallback `SuccessRedirectUrl` is configured, and no URL was supplied.
