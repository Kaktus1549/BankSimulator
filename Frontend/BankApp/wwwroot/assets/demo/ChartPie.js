async function PieFetch() {
  // Set new default font family and font color to mimic Bootstrap's default styling
  Chart.defaults.global.defaultFontFamily = '-apple-system,system-ui,BlinkMacSystemFont,"Segoe UI",Roboto,"Helvetica Neue",Arial,sans-serif';
  Chart.defaults.global.defaultFontColor = '#292b2c';
  console.log('Running');
  let apiData = [0, 0, 0];

  // Fetch data from the API /api/adminStats
  if(localStorage.getItem("role") == "Banker" || localStorage.getItem("role") == "User"){
    return;
  }

  let response = await fetch('/api/adminStats')
  if (response.status === 200){
    let data = await response.json();
    apiData[0] = data.totalUsersCount;
    apiData[1] = data.totalBankerCount;
    apiData[2] = data.totalAdminCount;

    localStorage.setItem('lastUpdated', data.lastUpdated);
  }
  else{
    let data = await response.json();
    let errors = data.errors || {}; // Ensure errors is defined
    let errorMessages = '';

    // Loop through errors
    for (const key in errors) {
        errorMessages += `${errors[key][0]}\n`;
    }
  }

  // Pie Chart Example
  var ctx = document.getElementById("myPieChart");
  while (ctx == null) {
    await new Promise(resolve => setTimeout(resolve, 100));
    ctx = document.getElementById("myPieChart");
  }
  var myPieChart = new Chart(ctx, {
    type: 'pie',
    data: {
      labels: ["Users", "Bankers", "Admins"],
      datasets: [{
        data: apiData,
        backgroundColor: ['#007bff', '#dc3545', '#ffc107'],
      }],
    },
  });
}

async function fetchLastUpdate() {
  // Try to get lastUpdate in localstorage, if not exist wait until its there
  while(!localStorage.getItem('lastUpdated')){
    await new Promise(resolve => setTimeout(resolve, 100));
  }
  return localStorage.getItem('lastUpdated');
}

async function BankerPieFetch() {
  // Set new default font family and font color to mimic Bootstrap's default styling
  Chart.defaults.global.defaultFontFamily = '-apple-system,system-ui,BlinkMacSystemFont,"Segoe UI",Roboto,"Helvetica Neue",Arial,sans-serif';
  Chart.defaults.global.defaultFontColor = '#292b2c';
  console.log('Running');
  let bankerApiData = [0, 0, 0];

  // Fetch data from the API /api/bankerStats

  let response = await fetch('/api/bankerStats')
  if (response.status === 200){
    let data = await response.json();
    bankerApiData[0] = data.freeAccountCount;
    bankerApiData[1] = data.savingAccountCount;
    bankerApiData[2] = data.creditAccountCount;
  }
  else{
    let data = await response.json();
    let errors = data.errors || {}; // Ensure errors is defined
    let errorMessages = '';

    // Loop through errors
    for (const key in errors) {
        errorMessages += `${errors[key][0]}\n`;
    }
  }

  console.log(bankerApiData);

  // Pie Chart Example
  var ctx = document.getElementById("myPieChart");
  while (ctx == null) {
    await new Promise(resolve => setTimeout(resolve, 100));
    ctx = document.getElementById("myPieChart");
  }
  var myPieChart = new Chart(ctx, {
    type: 'pie',
    data: {
      labels: ["Free accounts", "Saving accounts", "Credit accounts"],
      datasets: [{
        data: bankerApiData,
        backgroundColor: ['#007bff', '#dc3545', '#ffc107'],
      }],
    },
  });
}