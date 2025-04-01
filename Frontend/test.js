async function Reg(){
    // Send data to /api/register endpoint

    // Get data from input fields
    const inputFirstName = document.getElementById('inputFirstName');
    const inputLastName = document.getElementById('inputLastName');
    const inputEmail = document.getElementById('inputEmail');
    const inputPassword = document.getElementById('inputPassword');
    const inputPasswordConfirm = document.getElementById('inputPasswordConfirm');
    const inputUserType = document.getElementById('inputUserType');

    // Check if any of the input fields are empty
    if (!NullCheck(inputFirstName.value, inputLastName.value, inputEmail.value, inputPassword.value, inputPasswordConfirm.value)) {
        return;
    }
    // Check if email is valid
    if (!EmailCheck(inputEmail.value)) {
        alert('Invalid email format');
        return;
    }

    let roleId = inputUserType.value === 'User' ? 1 : inputUserType.value === 'Banker' ? 2 : 3;

    // Check if password and confirm password match
    if (inputPassword.value !== inputPasswordConfirm.value) {
        alert('Passwords do not match');
    }

    // Create a new user object
    const newUser = {
        firstName: inputFirstName.value,
        lastName: inputLastName.value,
        email: inputEmail.value,
        password: inputPassword.value,
        role: roleId
    };

    // Send data to /api/register endpoint
    let response = await fetch('/api/register', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(newUser)
    })
    console.log(response);
    if (response.status === 200) {
        alert('User created successfully');
        // Redirect to login page
        window.location.href = '/';
    } else {
        let data = await response.json();
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
        let errors = data.errors;
        let errorMessages = '';

        // Loop through errors
        for (const key in errors) {
            errorMessages += `${errors[key].split(': ')[1] }\n`;
        }

        // Show error messages
        alert(errorMessages);
    }
}