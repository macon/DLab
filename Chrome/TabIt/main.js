var port = null;

var getKeys = function(obj){
   var keys = [];
   for(var key in obj){
      keys.push(key);
   }
   return keys;
}

function appendMessage(text) {
  document.getElementById('response').innerHTML += "<p>" + text + "</p>";
}

function updateUiState() {
  if (port) {
    document.getElementById('connect-button').style.display = 'none';
    document.getElementById('input-text').style.display = 'block';
    document.getElementById('send-message-button').style.display = 'block';
  } else {
    document.getElementById('connect-button').style.display = 'block';
    document.getElementById('input-text').style.display = 'none';
    document.getElementById('send-message-button').style.display = 'none';
  }
}

function sendInputTextAsNativeMessage() {
  message = {"text": document.getElementById('input-text').value};
  sendNativeMessage(message);
  appendMessage("Sent message: <b>" + JSON.stringify(message) + "</b>");
}

function sendNativeMessage(message){
  port.postMessage(message);
}

function onNativeMessage(message) {
  appendMessage("Received message: <b>" + JSON.stringify(message) + "</b>");
  if (message === "get-tabinfo"){
    getTabInfo(sendTabInfoToNativePort)
  }
}

function sendTabInfoToNativePort(tabInfo){
  sendNativeMessage(tabInfo);
}

function onDisconnected() {
  appendMessage("Failed to connect: " + chrome.runtime.lastError.message);
  port = null;
  updateUiState();
}


function connect() {
  getTabInfo(
    function(tabInfo){
      appendMessage("have " + tabInfo.length + " tabs");
  });

  var hostName = "com.macon.dlab.tabit";
  appendMessage("Connecting to native messaging host <b>" + hostName + "</b>")
  port = chrome.runtime.connectNative(hostName);
  port.onMessage.addListener(onNativeMessage);
  port.onDisconnect.addListener(onDisconnected);
  updateUiState();
}

function getTabInfo(callBack){
  var tabInfo = new Array();
  var queryInfo = {
      active: true,
  };

  chrome.tabs.query({}, 
    function(tabs) {
      for (var i = 0; i < tabs.length; i++) {
        var tab = tabs[i];
        tabInfo.push({"id": tab.id, "url": tab.url, "title": tab.title});
      };
      callBack(tabInfo);
  });
}

document.addEventListener('DOMContentLoaded', 
  function () {
    document.getElementById('connect-button').addEventListener(
        'click', connect);
    document.getElementById('send-message-button').addEventListener(
        'click', sendInputTextAsNativeMessage);
    updateUiState();
});