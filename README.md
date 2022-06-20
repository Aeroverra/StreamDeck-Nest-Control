NestControl
====

The Nest Control pluginallows you to integrate your [Google Nest](https://store.google.com/us/category/connected_home?) devices with Stream Deck. This integration uses the [Smart Device Management](https://developers.google.com/nest/device-access/api) API and Google’s Cloud Pubsub to efficiently listen for changes in device state or other events. See [Supported Devices](https://developers.google.com/nest/device-access/supported-devices) for all devices supported by the SDM API.

There is currently only support for climate control devices like the Nest Thermostat.

You are in control of the information and capabilities exposed to Nest Control. You can authorize a single device or multiple devices.

`The Nest Smart Device Management (SDM) API requires a US$5 fee. (Paid To Google)`

Device Access Registration
---------------------------------------------------------

For the first phase, you will turn on the API and create the necessary credentials to have Nest Control talk to the Nest API.



**Create and configure Cloud Project \[Cloud Console\]**

By the end of this section you will have a Cloud Project with the necessary APIs enabled

1. Go to the [Google Cloud Console](https://console.developers.google.com/apis/credentials).

2. If this is your first time here, you likely need to create a new Google Cloud project. Click **Create Project** then **New Project**. ![Screenshot of APIs and Services Cloud Console with no existing project](https://i.imgur.com/geBhJyA.png)

3. Give your Cloud Project a name then click **Create**.

4. Go to [APIs & Services > Library](https://console.cloud.google.com/apis/library) where you can enable APIs.

5. From the API Library search for [Smart Device management](https://console.cloud.google.com/apis/library/smartdevicemanagement.googleapis.com) and click **Enable**.
   
   ![Screenshot of Search for SDM API](https://i.imgur.com/XIatTYq.png)

6. From the API Library search for [Cloud Pub/Sub API](https://console.developers.google.com/apis/library/pubsub.googleapis.com) in the Cloud Console and click **Enable**.

7. Go to the [Google Cloud Console Dashboard](https://console.cloud.google.com/)

8. Here you will see your Cloud Project Id. Copy it and place it into the Nest Control configuration
   
   ![](https://i.imgur.com/qg8UngH.png)

You now have a cloud project ready for the next section to configure authentication with OAuth.



**Configure OAuth Consent screen \[Cloud Console\]**

By the end of this section you will have configured the OAuth Consent Screen, needed for giving Nest Control access to your cloud project.

1. Go to the [Google API Console](https://console.developers.google.com/apis/credentials).

2. Click [OAuth consent screen](https://console.cloud.google.com/apis/credentials/consent) and configure it.

3. Select **External** (the only choice if you are not a G-Suite user) then click **Create**. While you are here, you may click the _Let us know what you think_ to give Google’s OAuth team any feedback about your experience configuring credentials for self-hosted software. They make regular improvements to this flow and appear to value feedback. ![Screenshot of OAuth consent screen creation](https://i.imgur.com/6pglo6F.png)

4. The _App Information_ screen needs you to enter an **App name** and **User support email**, then enter your email again under **Developer contact email**. These are only shown while you later go through the OAuth flow to authorize Nest Control to access your account. Click **Save and Continue**. Omit unnecessary information (e.g. logo) to avoid additional review by Google.

5. On the _Scopes_ step click **Save and Continue**.

6. On the _Test Users_ step, you need to add your Google Account (e.g., your @gmail.com address) to the list. Click _Save_ on your test account then **Save and Continue** to finish the consent flow. ![Screenshot of OAuth consent screen test users](https://i.imgur.com/e51NsUG.png)

7. Navigate back to the _OAuth consent screen_ and click **Publish App** to set the _Publishing status_ is **In Production**.
   
   ![Screenshot of OAuth consent screen production status](https://i.imgur.com/GFAe3du.png)

8. The warning says your _app will be available to any user with a Google Account_ which refers to the fields you entered on the _App Information_ screen if someone finds the URL. This does not expose your Google Account or Nest data.

9. Make sure the status is not _Testing_, or you will get logged out every 7 days.



**Configure OAuth client\_id and client\_secret \[Cloud Console\]**

By the end of this section you will have the `client_id` and `client_secret` which you need to add to the plugin configuration.

The steps below use _Web Application Auth_ with Nest Control to handle Google’s strict URL validation rules.

1. Navigate to the [Credentials](https://console.cloud.google.com/apis/credentials) page and click **Create Credentials**. ![Screenshot of APIs and Services Cloud Console](https://i.imgur.com/NNQRsU3.png)

2. From the drop-down list select _OAuth client ID_. ![Screenshot of OAuth client ID selection](https://i.imgur.com/dXyIk7i.png)

3. Enter _Web Application_ for the Application type.

4. Pick a name for your credential.

5. Add **Authorized redirect URIs** and enter `http://localhost:20777`

6. Click _Create_ to create the credential. ![Screenshot of creating OAuth credentials](https://i.imgur.com/qmfCCW8.png)

7. You should now be presented with an _OAuth client created_ message. You should now see your _Your Client ID_ and _Your Client Secret_. Add these to your Nest Control configuration.![Screenshot of OAuth Client ID and Client Secret](https://i.imgur.com/zsa6qvM.png)



**Create a Device Access project\_id \[Device Access Console\]**

Now that you have authentication configured, you will create a Nest Device Access Project which _requires a US$5 fee_. Once completed, you will have a device access `project_id` needed for later steps.

1. Go to the [Device Access Registration](https://developers.google.com/nest/device-access/registration) page. Click on the button [Go to the Device Access Console](https://console.nest.google.com/device-access/). ![Screenshot of Device Access Registration](https://i.imgur.com/s9xBifZ.png)

2. Check the box to “Accept the Terms of Service” and click **Continue to Payment** where you need to pay a fee (currently US$5). ![Screenshot of accepting terms](https://i.imgur.com/NkW7DJg.png)
   
   `It is currently not possible to share/be invited to a home with a G-Suite account. Make sure that you pay the fee with an account that has access to your devices.`

3. Now the [Device Access Console](https://console.nest.google.com/device-access/project-list) should be visible. Click on **Create project**.

4. Give your Device Access project a name and click **Next**. ![Screenshot of naming a project](https://i.imgur.com/jEfHNlw.png)

5. Next you will be asked for an _OAuth client ID_ which you created in the previous step and click **Next**. ![Screenshot of Device Access Console OAuth client ID](https://i.imgur.com/LPgc5iW.png)

6. Enable Events by clicking on **Enable** and **Create project**. ![Screenshot of enabling events](https://i.imgur.com/G7hjC6x.png)

7. You will now see a _Project ID_. Place this in your Nest Control configuration. At this point you have the `Cloud Project Id`, `Client Id` ,`Client Secret` and `Project Id` which is everything you need for configuration.
   
   
   
   **OAuth and Device Authorization steps**
   
   In this section you will authorize Home Assistant to access your account by generating an *Authentication Token*.
   
   1. Click the setup button in the configuration)
      
      ![](https://i.imgur.com/Reou3Li.png)
   
   2. If you have multiple accounts make sure you select the Google account your Nest is linked which should be the same account you setup the API with.
   
   3. The *Google Nest permissions* screen will allow you to choose which devices to configure and lets you select devices from multiple homes. You likely want to enable everything, however, you can leave out any feature you do not wish to use with Nest Control.
      
      ![](https://i.imgur.com/wADQ9L5.png)
   
   4. You will get redirected to another account selection page.
   
   5. You may see a warning screen that says *Google hasn’t verified this app* since you just set up an un-verified developer workflow. Click *Continue* to proceed.
   
   ![](https://i.imgur.com/HRLFtPO.png)

8. You may be asked to grant access to additional permissions. Click *Allow*

9. Confirm you want to allow persistent access to Nest Control.
   
   ![](https://i.imgur.com/tN6ip08.png)

10. You should now see a success message. Congratulations you have finished the plugin setup!

![](https://i.imgur.com/rAWiBqb.png)




