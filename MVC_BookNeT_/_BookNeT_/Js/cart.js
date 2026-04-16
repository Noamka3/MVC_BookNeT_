// פונקציה לעדכון אייקון העגלה
function updateCartIcon() {
    $.get('/ShoppingCart/GetCartCount', function (response) {
        const countElement = $('.cart-count');
        const count = response.count || 0;

        countElement.text(count);
        countElement.show();

        // אנימציה לעדכון המספר
        countElement.css('animation', 'none');
        countElement[0].offsetHeight; // טריגר לעדכון מחדש
        countElement.css('animation', 'pop 0.3s ease');
    });
}

$(document).ready(function () {
    // עדכון ראשוני של אייקון העגלה
    updateCartIcon();
});
