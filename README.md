# Portal Unity SDK


## Android setup

On Android, we utilize Chrome [Custom Tabs](https://developer.chrome.com/docs/android/custom-tabs/) (if available) to seamlessly connect gamers to Identity from within the game.

1. In Unity go to Build Settings -> Player Settings -> Android -> Publishing Settings -> Enable Custom Main Manifest and Custom Main Gradle Template under the Build section
2. Open the newly generated Assets/Plugins/Android/AndroidManifest.xml file. Add the following code inside the <application> element:

```
<activity
  android:name="com.portal.unity.RedirectActivity"
  android:exported="true" >
  <intent-filter android:autoVerify="true">
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />
    <data android:scheme="mygame" android:host="callback" />
    <data android:scheme="mygame" android:host="logout" />
  </intent-filter>
</activity>
```

3. Open the newly generated Assets/Plugins/Android/mainTemplate.gradle file. Add the following code inside dependencies block:

```
implementation('androidx.browser:browser:1.5.0')
```


For this version of the Chrome Custom Tabs to work, the compileSdkVersion must be at least 33. This is usually the same value as the targetSdkVersion, which you can set in Build Settings -> Player Settings -> Android -> Other Settings -> Target API Level.