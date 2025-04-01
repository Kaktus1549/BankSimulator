async function ChartBar(fetchMail=null) {
  // Set new default font family and font color to mimic Bootstrap's default styling
  Chart.defaults.global.defaultFontFamily = '-apple-system,system-ui,BlinkMacSystemFont,"Segoe UI",Roboto,"Helvetica Neue",Arial,sans-serif';
  Chart.defaults.global.defaultFontColor = '#292b2c';

  if (localStorage.getItem("role") == "admin" || localStorage.getItem("role") == "banker") {
    return;
  }

  // Fetch data from the API (/api/getMonthly) POST in body email: <useremail> from localStorage
  // Response will be array of 12 numbers

  let apiData = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

  let mail = fetchMail || localStorage.getItem('email')
  
  await fetch('/api/getMonthly', {
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




  // Bar Chart Example
  var ctx = document.getElementById("myBarChart");
  while (ctx == null) {
    ctx = document.getElementById("myBarChart");
    // Wait for the element to be available
    await new Promise(resolve => setTimeout(resolve, 100));
  }
  var myLineChart = new Chart(ctx, {
    type: 'bar',
    data: {
      labels: ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"],
      datasets: [{
        label: "Balance",
        backgroundColor: "rgba(2,117,216,1)",
        borderColor: "rgba(2,117,216,1)",
        data: apiData,
      }],
    },
    options: {
      scales: {
        xAxes: [{
          time: {
            unit: 'month'
          },
          gridLines: {
            display: false
          },
          ticks: {
            maxTicksLimit: 6
          }
        }],
        yAxes: [{
          ticks: {
            min: 0,
            max: 15000,
            maxTicksLimit: 5
          },
          gridLines: {
            display: true
          }
        }],
      },
      legend: {
        display: false
      }
    }
  });
}