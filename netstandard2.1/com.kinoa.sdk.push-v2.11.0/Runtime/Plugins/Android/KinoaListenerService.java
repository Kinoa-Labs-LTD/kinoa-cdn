package com.kinoa.android.plugins;

import android.util.Log;
import com.google.firebase.messaging.cpp.ListenerService;
import com.google.firebase.messaging.RemoteMessage;
import com.google.firebase.MessagingUnityPlayerActivity;
import com.kinoa.android.kinoanotificationmanager.KinoaNotificationManager;

public class KinoaListenerService extends ListenerService {
	@Override
	public void onMessageReceived(RemoteMessage message) {
	    Log.d("KinoaListenerService", "A message has been received.");
	    if (!KinoaNotificationManager.createNotification(getApplicationContext(), message, MessagingUnityPlayerActivity.class)) {
		    super.onMessageReceived(message);
	    }
	}
}