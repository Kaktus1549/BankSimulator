// Function to check if email is valid
function EmailCheck(email) {
    // Regular expression for validating email
    const emailRegex = /^[\w\-\.]+@([\w-]+\.)+[\w-]{2,}$/gm;
    return emailRegex.test(email);
}