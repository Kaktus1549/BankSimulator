async function ChartArea(fetchMail=null) {
  // Set new default font family and font color to mimic Bootstrap's default styling
  Chart.defaults.global.defaultFontFamily = '-apple-system,system-ui,BlinkMacSystemFont,"Segoe UI",Roboto,"Helvetica Neue",Arial,sans-serif';
  Chart.defaults.global.defaultFontColor = '#292b2c';

  if (localStorage.getItem("role") == "admin" || localStorage.getItem("role") == "banker") {
    return;
  }
  let apiData = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]

  // Create array of last 30 days [10.4, 11.4, ...] based on current date
  let today = new Date();
  let last30Days = [];
  for (let i = 0; i < 30; i++) {
    let date = new Date(today);
    date.setDate(today.getDate() - i);
    last30Days.push(date.toLocaleDateString('en-US', { month: '2-digit', day: '2-digit' }));
  }

  let mail = fetchMail || localStorage.getItem('email');

  await fetch('/api/getDaily', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({

      // try fetchmail, if not, fetch email
      email: mail
    })
  })
    .then(response => {
      if (!response.ok) {
        throw new Error('Network response was not ok');
      }
      return response.json();
    })
    .then(data => {
      console.log(data);
      if (data.result) {
        apiData = data.result || apiData; // Update apiData with fetched values
      } else {
          // Example response
          // {
          //     "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
          //     "title": "One or more validation errors occurred.",
          //     "status": 400,
          //     "errors": {
          //         "Email": [
          //             "Email is required"
          //         ]
          //     },
          //     "traceId": "00-8b821b07be43c31645523da4400924ee-fc7aa8e5b69f365e-00"
          // }

          // Get error messages
          let errors = data.errors; // Corrected reference to data.errors
          let errorMessages = '';

          // Loop through errors
          for (const key in errors) {
              errorMessages += `${errors[key][0]}\n`;
          }

          // Show error messages
          alert(errorMessages);
      }
    })
    .catch(error => {
      // Handle any errors
      console.error('There was a problem with the fetch operation:', error);
    });

    console.log(apiData);
  // Area Chart Example
  var ctx = document.getElementById("myAreaChart");
  while (ctx == null) {
    ctx = document.getElementById("myAreaChart");
    // Wait for the element to be available
    await new Promise(resolve => setTimeout(resolve, 100));
  }
  var myLineChart = new Chart(ctx, {
    type: 'line',
    data: {
      labels: last30Days,
      datasets: [{
        label: "Balance",
        lineTension: 0.3,
        backgroundColor: "rgba(2,117,216,0.2)",
        borderColor: "rgba(2,117,216,1)",
        pointRadius: 5,
        pointBackgroundColor: "rgba(2,117,216,1)",
        pointBorderColor: "rgba(255,255,255,0.8)",
        pointHoverRadius: 5,
        pointHoverBackgroundColor: "rgba(2,117,216,1)",
        pointHitRadius: 50,
        pointBorderWidth: 2,
        data: apiData,
      }],
    },
    options: {
      scales: {
        xAxes: [{
          time: {
            unit: 'date'
          },
          gridLines: {
            display: false
          },
          ticks: {
            maxTicksLimit: 7
          }
        }],
        yAxes: [{
          ticks: {
            min: 0,
            max: 40000,
            maxTicksLimit: 5
          },
          gridLines: {
            color: "rgba(0, 0, 0, .125)",
          }
        }],
      },
      legend: {
        display: false
      }
    }
  });
}

async function BankerChart() {
  // Set new default font family and font color to mimic Bootstrap's default styling
  Chart.defaults.global.defaultFontFamily = '-apple-system,system-ui,BlinkMacSystemFont,"Segoe UI",Roboto,"Helvetica Neue",Arial,sans-serif';
  Chart.defaults.global.defaultFontColor = '#292b2c';

  if (localStorage.getItem("role") == "admin" || localStorage.getItem("role") == "banker") {
    return;
  }
  let apiDataDays = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]

  // Create array of last 30 days [10.4, 11.4, ...] based on current date
  let today = new Date();
  let last30Days = [];
  for (let i = 0; i < 30; i++) {
    let date = new Date(today);
    date.setDate(today.getDate() - i);
    last30Days.push(date.toLocaleDateString('en-US', { month: '2-digit', day: '2-digit' }));
  }
  let response = await fetch('/api/bankerStats')
  if (response.status === 200){
    let data = await response.json();
    apiDataDays = data.TotalDebt30Days || apiDataDays; // Update apiData with fetched values
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
  // Area Chart Example
  var ctx = document.getElementById("myAreaChart");
  while (ctx == null) {
    ctx = document.getElementById("myAreaChart");
    // Wait for the element to be available
    await new Promise(resolve => setTimeout(resolve, 100));
  }
  var myLineChart = new Chart(ctx, {
    type: 'line',
    data: {
      labels: last30Days,
      datasets: [{
        label: "Balance",
        lineTension: 0.3,
        backgroundColor: "rgba(2,117,216,0.2)",
        borderColor: "rgba(2,117,216,1)",
        pointRadius: 5,
        pointBackgroundColor: "rgba(2,117,216,1)",
        pointBorderColor: "rgba(255,255,255,0.8)",
        pointHoverRadius: 5,
        pointHoverBackgroundColor: "rgba(2,117,216,1)",
        pointHitRadius: 50,
        pointBorderWidth: 2,
        data: apiDataDays,
      }],
    },
    options: {
      scales: {
        xAxes: [{
          time: {
            unit: 'date'
          },
          gridLines: {
            display: false
          },
          ticks: {
            maxTicksLimit: 7
          }
        }],
        yAxes: [{
          ticks: {
            min: 0,
            max: 40000,
            maxTicksLimit: 5
          },
          gridLines: {
            color: "rgba(0, 0, 0, .125)",
          }
        }],
      },
      legend: {
        display: false
      }
    }
  });
}