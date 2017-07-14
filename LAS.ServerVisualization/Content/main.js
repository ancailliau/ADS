
//var mymap = L.map('mapid').setView([50.8234575,4.3831473], 14);
var mymap = L.map('mapid').setView([50.8234575,4.3831473], 14);
L.tileLayer('https://api.tiles.mapbox.com/v4/{id}/{z}/{x}/{y}.png?access_token={accessToken}', {
    attribution: 'Map data &copy; <a href="http://openstreetmap.org">OpenStreetMap</a> contributors, <a href="http://creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>, Imagery © <a href="http://mapbox.com">Mapbox</a>',
    maxZoom: 18,
    id: 'mapbox.light',
    accessToken: 'pk.eyJ1IjoiYW5jYWlsbGlhdSIsImEiOiJjajUzbW90NXQwNnF6MzJtd3RrdzB6OWppIn0.SR-d22ltbfWMD_LxkwR4HQ'
}).addTo(mymap);

// L.tileLayer('https://api.tiles.mapbox.com/v4/{id}/{z}/{x}/{y}.png?access_token={accessToken}' + accessToken, {
// 	attribution: 'Map data &copy; <a href="http://openstreetmap.org">OpenStreetMap</a> contributors, <a href="http://creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>, Imagery © <a href="http://mapbox.com">Mapbox</a>',
//     maxZoom: 18,
//     id: 'mapbox.streets',
//     accessToken: 'pk.eyJ1IjoiYW5jYWlsbGlhdSIsImEiOiJjajUzbW90NXQwNnF6MzJtd3RrdzB6OWppIn0.SR-d22ltbfWMD_LxkwR4HQ'
// }).addTo(mymap);

var ambulances = {};
var incidents = {};

var ambulanceColor = function (status) {
	if (status == 0) return 'green'; // available at station
	if (status == 1) return 'orange'; // leaving
	if (status == 2) return 'blue'; // on scene
	if (status == 3) return 'blue'; // to hospital
	if (status == 4) return 'navy'; // at hospital
	if (status == 5) return 'green'; // available radio
	if (status == 6) return 'red'; // unavailable
}

var incidentColor = function (status) {
	if (status == 0) return 'red';  // not resolved
	if (status == 1) return 'grey'; // resolved
}

var ambulanceStatusName = function(status) {
	if (status == 0) return 'available at station';
	if (status == 1) return 'leaving'; // leaving
	if (status == 2) return 'on scene'; // on scene
	if (status == 3) return 'to hospital'; // to hospital
	if (status == 4) return 'at hospital'; // at hospital
	if (status == 5) return 'available radio'; // available radio
	if (status == 6) return 'unavailable'; // unavailable
}

var updateMap = function () {
	// console.log("Update");
	updateAmbulances ();
	updateIncidents ();
	updateAllocations ();
}

var updateIncidents = function () {
	$.getJSON( "/Home/Incidents", function( data ) {
		data.sort(function(a, b){
    		if(a.IncidentId < b.IncidentId) return -1;
    		if(a.IncidentId > b.IncidentId) return 1;
    		return 0;
		});

		var items = [];
		$.each( data, function( key, val ) {
			if (!incidents.hasOwnProperty(val.IncidentId)) {
				// console.log(val);
				var m = L.circle(
						[val.Latitude, val.Longitude], 10, {
						color: incidentColor(val.Resolved),
						fillColor: incidentColor(val.Resolved),
						fillOpacity: 0.5
					});
		    	incidents[val.IncidentId] = m;
		    	m.addTo(mymap);
		    	m.bindPopup("<b>Incident "+val.IncidentId+"</b><br />Resolved: "+val.Resolved+".");
		    } else {
		    	var m = incidents[val.IncidentId];
		    	m.setLatLng([val.Latitude, val.Longitude]);
		    	m.setStyle ({ color: incidentColor(val.Resolved),
		    				  fillColor: incidentColor(val.Resolved) });
				m.getPopup().setContent("<b>Incident "+val.IncidentId+"</b><br />Resolved: "+val.Resolved+".");
		    }

    		items.push( "<tr><td>"+val.IncidentId+"</td><td>"+val.Resolved+"</td></tr>" );
		});
		$("#incidentTable").replaceWith($('<tbody/>', { id: "incidentTable", html: items.join("")}));
	});
};


var updateAmbulances = function () {
	$.getJSON( "/Home/Ambulances", function( data ) {
		data.sort(function(a, b){
    		if(a.AmbulanceId < b.AmbulanceId) return -1;
    		if(a.AmbulanceId > b.AmbulanceId) return 1;
    		return 0;
		});

		var items = [];
		$.each( data, function( key, val ) {
			if (!ambulances.hasOwnProperty(val.AmbulanceId)) {
				var m = L.circle(
					[val.Latitude, val.Longitude], 20, {
						color: ambulanceColor(val.Status),
						fillColor: ambulanceColor(val.Status),
						fillOpacity: 0.5
					});

		    	ambulances[val.AmbulanceId] = m;
		    	m.addTo(mymap);
		    	m.bindPopup("<b>Ambulance "+val.AmbulanceId+"</b><br />Status: "+ambulanceStatusName(val.Status)+".");
		    } else {
		    	var m = ambulances[val.AmbulanceId];
		    	m.setLatLng([val.Latitude, val.Longitude]);
		    	m.setStyle ({
		    		color: ambulanceColor(val.Status),
					fillColor: ambulanceColor(val.Status)
				});
		    	m.getPopup().setContent("<b>Ambulance "+val.AmbulanceId+"</b><br />Status: "+ambulanceStatusName(val.Status)+".");
		    	// console.log("Ambulance " + val.Identifier + " updates position: " + [val.Latitude, val.Longitude]);
		    }

    		items.push( "<tr><td>"+val.AmbulanceId+"</td><td>"+ambulanceStatusName(val.Status)+"</td><td>"+val.InTrafficJam+"</td></tr>" );
		});
		$("#ambulanceTable").replaceWith($('<tbody/>', { id: "ambulanceTable", html: items.join("")}));
	});
};

var formatDate = function(date) {
	if (date == null) return "N.A.";
	var a = new Date(date);
	return a.getHours () + ":" + a.getMinutes() + ":" + a.getSeconds() + "." + a.getMilliseconds ();
}

var updateAllocations = function () {
	$.getJSON( "/Home/Allocations", function( data ) {
		data.sort(function(a, b){
    		if(a.AllocationId < b.AllocationId) return -1;
    		if(a.AllocationId > b.AllocationId) return 1;
    		return 0;
		});

		var items = [];
		$.each( data, function( key, val ) {
    		items.push(
    			"<tr>"
    			+ "<td>"+val.AllocationId+"</td>"
    			+ "<td>"+val.IncidentId+"</td>"
      			+ "<td>"+val.AmbulanceId+"</td>"
      			+ "<td>"+val.HospitalId+"</td>"
      			+ "<td>"+formatDate(val.AllocationTimestamp)+"</td>"
      			+ "<td>"+formatDate(val.MobilizationSentTimestamp)+"</td>"
      			+ "<td>"+formatDate(val.MobilizationReceivedTimestamp)+"</td>"
      			+ "<td>"+formatDate(val.MobilizationConfirmedTimestamp)+"</td>"
      			+ "<td>"+val.Refused+"</td>"
      			+ "<td>"+val.Cancelled+"</td>"
      			+ "<td>"+val.CancelConfirmed+"</td>"
      			+ "</tr>");
		});
		$("#allocationTable").replaceWith($('<tbody/>', { id: "allocationTable", html: items.join("")}));
	});
};

updateMap();
setInterval(updateMap, 50 * 100); // update every 50 ms