# Deployment Guide

## Overview
This document is created to help IT Administrators to deploy, configure, and use the **Shifts-Kronos Integration application** in a Microsoft Azure environment.  
**Kronos Workforce Central (Kronos WFC 8.1)** is a Workforce Management system designed for First Line Managers (FLMs) and First Line Workers (FLWs). Kronos provides various capabilities to handle schedules of FLWs in organizations with multiple dpeartments and job categories. First Line Workers can access their schedule, create schedule requests for Time-Offs, Open Shifts, Swap Shifts, etc.. FLMs can create, and access their FLWs' schedules, schedule requests and approve those.  
**Shifts App in Microsoft Teams** keeps FLWs connector and in sync. It's built mobile first for fast and effective time management and communication for teams. Shifts lets FLWs and FLMs use their mobile devices to manage schedules and keep in touch.  
**Shifts-Kronos Integration application** is built to sync data between Kronos Workforce Central (v8.1) and Microsoft Shifts App in Teams in seamless fashion. It helps FLWs access their schedules which are created in Kronos system from Shifts App, and further enables FLMs to access schedule requests which are created in Shifts from Kronos system.

## Considerations
The points noted below are to be considered as best practices to properly leverage the full potential of the Shifts-Kronos Integration application.

* IT Admin has functional understating of Kronos WFC 8.1 and Microsoft Teams Shifts App. IT Admin is also the super user of Kronos – The IT Admin needs to have admin-level access to Shifts as their credentials are required for request approval  
* Kronos WFC serves as single source of truth for all entities  
* Shifts App is used by FLWs to view their schedules, create requests for Time-Offs, Open-Shifts, Swap-Shifts  
* FLMs will use Kronos WFC only for all Approval/Rejection workflows  
* FLW requests (Open Shift Request, Swap Shift Request) will be sync’d from Shifts to Kronos in synchronous manner using Shifts Outbound APIs and Kronos WFC 8.1 data submission (POST) APIs  
* FLW requests for Time Off will be sync’d from Shifts to Kronos in asynchronous manner  
* Approved schedules for Shifts, Time-Offs, Open-Shifts and Swap-Shifts will be sync’d from Kronos to Shifts in asynchronous manner using Kronos WFC 8.1 GET APIs and Shifts/Graph post APIs  
* Status of requests created in Shifts App and synced to Kronos WFC will be synced back to Shifts to keep both systems in sync  
* To sync all the requests initiated in Shifts (by FLWs) to Kronos, SuperUser account credentials are used. Once these are approved in Kronos (by FLMs), their approval status will be synced back to Shifts. These statuses are synced to Shifts using Microsoft Graph APIs with Shifts Admin account authorization  
* Users must be created in Azure/Teams prior to User to User mapping step to be performed in Configuration Web App (Config Web App is one of the components of this integration as explained in below sections)  
* Teams and Scheduling groups must be created in Shifts prior Teams to Department mapping step in Configuration Web App  
* Done button on Configuration Web App should be used only for first time sync  
* First time sync is expected to take longer time since it may sync data for larger time interval. The time would vary based on amount of data i.e. number of users, number of teams, number of entities (such as Shifts, TimeOffs, OpenShifts etc.) to be synced and date span of the Time interval for which the sync is happening. So, it may take time to reflect this complete data in Shifts. Done button click will initiate background process to complete the sync  

## Solution Overview
The Shifts-Kronos Integration application has the following components built using ASP.Net Core 2.2. Those need to be hosted on Microsoft Azure.  
* Configuration Web App  
* Integration Service API  
* Azure Logic App for periodic data sync  
* Kronos WFC solution to retrieve data and post data, part of Integration Service API  

![architecture](images/arch-diagram.png)

1.	Azure Web App Services – For Configuration Web App and the Integration Service API. The Configuration Web App and the Integration Service API are both written in ASP.NET Core technologies
2.	Azure Table Storage – the database account which contains the necessary tables required for the entire Shifts-Kronos Integration to work successfully
3.	Azure Logic App – this is the schedule job that will sync data between Kronos WFC and Shifts on a configured interval of time or configured number of previous days from current date till number of next days, based on flag passed to APIs for slide dates or complete sync
4.	Azure Key Vault – to store all the connection strings, client Ids, client secrets, access token for accessing graph API (All the data which requires encryption must be the part of key vault)
5.	Kronos Solution – This is custom library project which is the part of Integration Service API. It will be used to query and submit data to Kronos WFC
6.	Application Insights – Capture necessary telemetry at the time of necessary events, and will be used by both the Configuration Web App, the Integration Service API

## Deployment
Following section explains necessary steps to deploy Shifts-Kronos Integration application

### Prerequisites
To begin with, you will need to ensure following perequisites:

1. Kronos WFC 8.1 - Access to Kronos WFC 8.1 System with following details:  
* Kronos WFC endpoint
* SuperUser Name
* SuperUser password  
Review and ensure users, org levels and jobs are properly setup in Kronos system

2. Microsoft Teams Shifts App - Access to Teams Deployment with Shifts App
* Tenant ID
* Tenant Admin credentials  
Review and ensure AAD users, Teams, and Scheduling Groups are properly setup in Teams Shifts App  

3. Microsoft Azure environment to host Shifts-Kronos Integration App - An Azure subscription where you can create the following resources:  
* App services
* App service plan
* Azure Table storage account
* Azure Blob storage
* Azure Key Value
* Application Insights

### Register Azure AD Application
This integration app uses [Microsoft Graph APIs](https://developer.microsoft.com/en-us/graph) to access users (FLWs & FLMs) and teams and their schedules from Microsoft Teams Shifts App. To use Microsoft Graph to read and write resources on behalf of a user, this integration app needs to be registered in Azure AD by following steps below.  This is required to use Microsoft identity platform endpoint for authentication and authorization with Microsoft Graph.
1.	Log in to the Azure Portal for your subscription, and go to the "App registrations" blade here
2.	Click on "New registration” and create an Azure AD application
* **Name**: The name of your Teams app - if you are following the template for a default deployment, we recommend "Shifts-Kronos Integration"
* **Supported account types**: Select "Accounts in any organizational directory (Any Azure AD directory - Multitenant)"
* **Redirect URI based on ADAL / MSAL**: The URIs that will be accepted as destinations when returning authentication responses (tokens) after successfully authenticating users

**Figure 1.** Azure AD Application Registration
![figure1](images/figure1.png)

3. Click on the "Register" button
4. When the app is registered, you'll be taken to the app's "Overview" page. Copy the **Application (client) ID**; we will need it later. Verify that the "Supported account types" is set to **Multiple organizations**

**Figure 2.** Azure Application Registration Overview page.
![figure2](images/figure2.png)

5. On the side rail in the Manage section, navigate to the "Certificates & secrets" section. In the Client secrets section, click on "+ New client secret". Add a description (Name of the secret) for the secret and select “Never” for Expires. Click "Add"

![figure3](images/figure3.png)

6. Once the client secret is created, copy its Value; we will need it later
7. Navigate to the Authentication page that can be found in the left blade in Figure 3
8. Under the section that reads *Implicit grant*, make sure that the check boxes for Access tokens and ID tokens are checked. The screen should resemble something like the screenshot that follows:

![figure4](images/figure4.png)

At this point you have the following unique values:
* Application (client) ID
* Client secret
* Directory (Tenant) ID
* Managed Object ID

We recommend that you copy these values into a text file, using an application like Notepad. You will need these values later during the application deployment process.

### Microsoft Graph API Permissions
The table below outlines the required permissions necessary for the Azure AD application registration to successfully work end-to-end. These Graph API permissions should have their consent provided on the app registration:

**Table 1.** The list of Microsoft Graph API permissions.

|Scope|Application/Delegated|Function|
|-----|---------------------|--------|
|Group.Read.All|Delegated|Allows application to list groups and read properties and all group memberships on behalf of the signed-in user (tenant admin).|
|Group.ReadWrite.All|Delegated|Allows the application to create groups and read all group properties and memberships on behalf of the signed-in user (tenant admin).|
|WorkforceIntegration.Read.All|Delegated|Allows for workforce integrations to be retrieved from Microsoft Graph.|
|WorkforceIntegration.ReadWrite.All|Delegated|Allows for workforce integrations to be created and registered with Microsoft Graph.|
|offline_access|N/A|Enables for the Microsoft Graph token to be automatically refreshed|
|Schedule.Read.All|Application|Read all schedule items.|
|Schedule.ReadWrite.All|Application|Read and write all schedule items.|

## Deploy Application to your Azure Subscription
Here are the following requirements to correctly deploy the **Shifts-Kronos Integration** application to your Azure subscription: 
1. An ARM Template published as part of the Microsoft GitHub package.
2. Ensure to **properly** fork the main Microsoft repo to your account. This helps in three ways:
   1. The forked copy does not impate the master branch in the Microsoft repository
   2. Having a forked copy of the main repository, tenant admins can deploy from the forked repo
   3. If there are changes that are required for your organization, you can always modify the code in your forked copy and re-deploy
3. GitHub package containing the code for the Configuration Web App and the Integration Service API
4. You will be prompted to click on the *Deploy to Azure* button below, and when prompted log in to your Azure subscription

[![Deploy to Azure](https://azuredeploy.net/deploybutton.png)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FOfficeDev%2FMicrosoft-Teams-Shifts-WFM-Connectors%2Fmaster%2FDeployment%2Fazuredeploy.json)  
5. It will navigate to the form for filling the template parameters  
6. Select a subscription and a resource group - it is recommended to create a new resource group  
7. Fill in the values for the parameters of the ARM Template. They are defined in the table below:

**Table 2.** The ARM Template parameters
|Parameter Name|Description|
|--------------|-----------|
|baseResourceName|This is a standard name to be used across all resources.|
|aadAppClientId|This is the Client ID from the app registration.|
|aadAppClientSecret|This is the Client Secret from the app registration.|
|managedAadAppObjectId|This is the AAD Object ID of the app registration.|
|teamsTenantId|This is the Tenant ID of your Teams tenant, and it can be different than the tenant ID of the Azure sub where the Shifts-Kronos Integration package is deployed.|
|firstTimeSyncStartDate|This is the start date of the first-time data sync between Kronos and Shifts.|
|firstTimeSyncEndDate|This is the end date of the first-time data sync between Kronos and Shifts.|
|Location|This is the data center for all the resources that will be deployed through the ARM Template. Make sure that you select a location that can host Application Insights, Azure Table Storage, Azure Key Vault, and Redis Cache.|
|storageAccountType|This is the storage grade for the Azure table storage account.|
|Sku|This is the payment tier of the various resources.|
|planSize|The size of the hosting plan required for the API web app service and the Configuration Web App service.|
|processNumberOfUsersInBatch|When syncing the shift entities between Kronos and Shifts, the transfer is done based on users in a batch manner. The default value is 100 and can be changed at the time of deployment.|
|processNumberOfOrgJobsInBatch|When syncing the open shift entities between Kronos and Shifts, the transfer is done based on the org job paths in a batch manner. The default value is 50 and can be changed at the time of deployment.|
|syncFromPreviousDays|The number of days in the past for subsequent syncs between Kronos and Shifts.|
|syncToNextDays|The number of days in the future for subsequent syncs between Kronos and Shifts.|
|gitRepoUrl|The public GitHub repository URL.|
|gitBranch|The specific branch from which the code can be deployed. The recommended value is master, however, at the time of deployment this value can be changed.|

8.	Agree to the Azure terms and conditions by clicking on the check box *I agree to the terms and conditions stated above* located at the bottom of the page
9.	Click on *Purchase* to start the deployment
10.	Wait for the deployment to finish. You can check the progress of the deployment from the *Notifications* pane of the Azure Portal
11.	Once the deployment has finished, you would have the option to navigate to the resource group to ensure all resources are deployed correctly
12.	Smoke test – this step is required to ensure that all the code has been properly deployed

## Post ARM Template Deployment Steps
The following actions are to be done post deployment to ensure that all the information is being exchanged correctly between the resources in the newly created resource group:
1.	Access Policy Setup in Azure Key Vault
2.	Setting up the Redirect URIs
3.	Logout URL setting in App Registration
4.	Uploading Excel files into Azure Blob storage
5.	User creation through the Teams Admin portal
6.	Establishing the necessary recurrence for the Azure Logic App

### Access Policy Setup in Azure Key Vault
1.	Using the system assigned identity for both the deployed API web app service and the deployed Configuration Web App service – this is taken care of through the ARM template
2.	Ensuring to have the principalId of the app registration as well for the access policy
3.	Outlining the details of establishing necessary AAD users to have access to the deployed Azure Key Vault

### Setting up the Redirect URIs
* Once the ARM Template deployment is successful, there would be an output screen that will show the necessary URL for the Configuration Web App service. Copy that URL into an application such as Notepad  
* Navigate to the App Registration created earlier  
* In Figure 2, click on the text next to the text that reads Redirect URIs  
* There is a chance that the screen may not have any redirect URIs. You would need to set those now. 

**Figure 5.** Redirect URIs being set already
![figure5](images/figure5.png)

* For any new app registrations, the redirect URIs may not be set
* You need to properly take the Configuration Web App service URL that is deployed as part of the ARM Template deployment and paste that URL here
* Subsequently, you need to have paste that same URL, and append “/signin-oidc”. With doing so, the tenant admin when logging into the Configuration Web App, will be authenticated using OpenIdConnect  
* Once all the changes are made, ensure to commit the changes through clicking on the button that reads Save

### Logout URL setting in App Registration
1.	Log on to the Azure portal
2.	Navigate to the application registration recently created (refer to the screenshots above)
3.	On the left-hand side, click on the option that reads Manifest
4.	In the code window that appears, scroll down until you read the JSON attribute: logoutUrl
Refer to the screenshot:

**Figure 6.** Application Registration Manifest window
![figure6](images/figure6.png)

5.	The value for the logoutUrl is the URL of the Configuration Web App service that was deployed through the ARM Template
6.	Copy and paste the URL from step 5 as the value for the logoutUrl
7.	Once the changes have been made, the save button at the top of the screenshot in step 4 will transition from a disabled state to an enabled state
8.	Click on the Save button to properly commit the changes made

### Uploading Excel Template files into Azure Blob Storage
Once the ARM Template deployment is successful, one final operation is to ensure that the Excel template files are uploaded to the Azure Blob storage that has been provisioned through the ARM Template. The steps below outline the procedure:

1.	Log onto the Azure portal, and sign in using your administrator credentials
2.	Navigate to the resource group that was created at the time of ARM Template deployment
3.	Navigate to the storage account that was created from the ARM Template deployment. The screen should resemble the figure below: 

**Figure 7.** Storage account overview
![figure7](images/figure7.png)

4.	Navigate into the containers, by clicking on the link that reads *Containers* from Figure 7 above
5.	Upon navigation to the containers, the ARM Template should provision a blob container called “templates”, and the screen should resemble below:

**Figure 8.** Templates blob container
![figure8](images/figure8.png)

6. Navigate inside of the "templates" blob container, and the screen should resemble the next screenshot below: 

**Figure 9.** Navigation inside of the "templates" blob.
![figure9](images/figure9.png)

User Creation through the Teams Admin Portal
1.    Navigate to the [Microsoft Teams Admin Portal](https://admin.teams.microsoft.com)
2.	Sign In with your AAD Tenant Admin credentials
3.	You will be presented with the following view once the sign in is successful:

**Figure 10.** Home page of the Teams Admin portal
![figure10](images/figure10.png)

4. From Figure 10, navigate to the Users page by clicking on the option that reads *Users* in the left hand blade. The screen should resemble the following below:

**Figure 11.** The users landing page
![figure11](images/figure11.png)

5. In the text above the table, there is a mention of "Admin center > Users". That is the location where you should go to add users to the tenant. Clicking on the "Admin center > Users" hyperlink in Figure 11 above should yield in the page below:

**Figure 12.** The page to add or remove users
![figure12](images/figure12.png)

### Setting up the Recurrence for the Azure Logic App
1.	Post ARM Template deployment, the outputs section would have necessary URLs
2.	Open the Logic App in the designer in another tab on your browser
3.	Add the recurrence
4.	Set the time on the recurrence
5.	Choose an integer value
6.	Choose an interval (i.e. Second, Minute, Hour, Day, Week, Month) – it is recommended to select minutes as you would want to give enough time to perform the sync operations (for ex: 15 mins)

## Configuration App Workflow
The Configuration Web App serves as a helpful aid to establish the necessary configurations to properly integrate an instance of Kronos WFC v8.1 with Shifts app. It further helps to create mapping between Kronos and Shifts users as well as Kronos departments and Shifts teams.

# Legal notice

Please read the license terms applicable to this [license](https://github.com/OfficeDev/Microsoft-Teams-Shifts-WFM-Connectors/blob/master/LICENSE). In addition to these terms, you agree to the following: 

* You are responsible for complying with all applicable privacy and security regulations, as well as all internal privacy and security policies of your company. You must also include your own privacy statement and terms of use for your app if you choose to deploy or share it broadly. 

* This template includes functionality to provide your company employees with HR information, and it is your responsibility to ensure the data is presented accurately. 

* Use and handling of any personal data collected by your app is your responsibility. Microsoft will not have any access to data collected through your app, and therefore is not responsible for any data related incidents.

# Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com. 

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA. 

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact opencode@microsoft.com with any additional questions or comments. 

 