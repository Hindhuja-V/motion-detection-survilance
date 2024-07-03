/*
 *
 *  Push Notifications codelab
 *  Copyright 2015 Google Inc. All rights reserved.
 *
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *      https://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License
 *
 */

/* eslint-env browser, es6 */

'use strict';

const applicationServerPublicKey = 'BEu09qCcFIreSF2qnR2W8pAKcFAn6wpJVFaKKx0BICpxevmLyGnrxxZFNOV0rJOyZifkgdxIxjhtNsYWREPJBNg';

const pushButton = document.querySelector('.js-push-btn');

let isSubscribed = false;
let swRegistration = null;

function urlB64ToUint8Array(base64String) {
  const padding = '='.repeat((4 - base64String.length % 4) % 4);
  const base64 = (base64String + padding)
    .replace(/\-/g, '+')
    .replace(/_/g, '/');

  const rawData = window.atob(base64);
  const outputArray = new Uint8Array(rawData.length);

  for (let i = 0; i < rawData.length; ++i) {
    outputArray[i] = rawData.charCodeAt(i);
  }
  return outputArray;
}

function updateBtn() {
  if (Notification.permission === 'denied') {
    pushButton.textContent = 'Push Messaging Blocked.';
    pushButton.disabled = true;
    updateSubscriptionOnServer(null);
    return;
  }

  if (isSubscribed) {
    pushButton.textContent = 'Disable Push Messaging';
  } else {
    pushButton.textContent = 'Enable Push Messaging';
  }

  pushButton.disabled = false;
}

function sendSubscriptionToServer(subscription) {
  let sub = JSON.parse(JSON.stringify(subscription));

  let path = url + '/sub' + `?endpoint=${sub.endpoint}&p256dh=${sub.keys.p256dh}&auth=${sub.keys.auth}`;

  console.log(path);

  if (confirm("Go back to controls?"))
    window.location.replace(path);
}



function updateSubscriptionOnServer(subscription) {
  const subscriptionJson = document.querySelector('.js-subscription-json');
  const subscriptionDetails =
    document.querySelector('.js-subscription-details');

  if (subscription) {
    subscriptionJson.textContent = JSON.stringify(subscription);
    //subscriptionDetails.classList.remove('is-invisible');
    sendSubscriptionToServer(subscription);
  } else {
    //subscriptionDetails.classList.add('is-invisible');
  }
}

function subscribeUser() {
  const applicationServerKey = urlB64ToUint8Array(applicationServerPublicKey);
  swRegistration.pushManager.subscribe({
      userVisibleOnly: true,
      applicationServerKey: applicationServerKey
    })
    .then(function (subscription) {
      console.log('User is subscribed.');

      updateSubscriptionOnServer(subscription);

      isSubscribed = true;

      updateBtn();
    })
    .catch(function (err) {
      console.log('Failed to subscribe the user: ', err);
      updateBtn();
    });
}

function unsubscribeUser() {
  swRegistration.pushManager.getSubscription()
    .then(function (subscription) {
      if (subscription) {
        return subscription.unsubscribe();
      }
    })
    .catch(function (error) {
      console.log('Error unsubscribing', error);
    })
    .then(function () {
      updateSubscriptionOnServer(null);

      console.log('User is unsubscribed.');
      isSubscribed = false;

      updateBtn();
    });
}

function initializeUI() {
  pushButton.addEventListener('click', function () {
    pushButton.disabled = true;
    if (isSubscribed) {
      unsubscribeUser();
    } else {
      subscribeUser();
    }
  });

  // Set the initial subscription value
  swRegistration.pushManager.getSubscription()
    .then(function (subscription) {
      isSubscribed = !(subscription === null);

      updateSubscriptionOnServer(subscription);

      if (isSubscribed) {
        console.log('User IS subscribed.');
      } else {
        console.log('User is NOT subscribed.');
      }

      updateBtn();
    });
}

if ('serviceWorker' in navigator && 'PushManager' in window) {
  console.log('Service Worker and Push is supported');

  navigator.serviceWorker.register('sw.js')
    .then(function (swReg) {
      console.log('Service Worker is registered', swReg);

      swRegistration = swReg;
      initializeUI();
    })
    .catch(function (error) {
      console.error('Service Worker Error', error);
    });
} else {
  console.warn('Push messaging is not supported');
  pushButton.textContent = 'Push Not Supported';
}





let url = "http://192.168.1.104:8081";
var preview = false;

const sleep = (ms) => {
  return new Promise(resolve => setTimeout(resolve, ms));
}

function sendReq() {
  let req = new XMLHttpRequest();
  req.onreadystatechange = function () {
    if (this.readyState == 4 && this.status == 200) {
      document.getElementById('img').setAttribute('src',
        'data:image/png;base64,' + this.responseText);
      if (preview) {
        //setTimeout(sendReq(), 20000);
        sleep(500).then(() => sendReq());
      }
    }
  }
  req.open("GET", url + '/image', true);
  req.send();
}

function startPrev() {
  console.log("yo")
  document.querySelector('#status').innerText = "Preview Started"
  preview = true;
  sendReq();
}

function stopPrev() {
  document.querySelector('#status').innerText = "Preview Stopped"
  preview = false;
}

function sendData(val) {
  let turl = url + val
  let req = new XMLHttpRequest();
  req.open("GET", turl, true);
  req.send();
}

function setIpFromParam() {
  let params = (new URL(document.location)).searchParams;
  url = params.get('ip');
  document.querySelector("#ip").value = url;
}

function setIpFromHost() {
  let params = (new URL(document.location)).searchParams;
  url = 'http://' + window.location.host;
  document.querySelector("#ip").value = url;
}

function SubToGithub() {
  window.location.replace("https://ankur198.github.io/MotionDetectionSurvilance/?ip=" + url);
}