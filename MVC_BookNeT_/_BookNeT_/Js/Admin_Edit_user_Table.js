$(document).ready(function () {
    $('.userEditsBtn').click(function () {
        var userId = $(this).data('id'); // קבלת ה-ID מתוך כפתור

        // שליחת בקשה לשרת לקבלת פרטי המשתמש
        $.ajax({
            url: `/Users/Edits/${userId}`, // שימוש בנתיב מוחלט
            type: 'GET',
            success: function (data) {
                // הצגת פרטי המשתמש בתוך ה-Modal
                $('#userEditsContent').html(data);
            },
            error: function () {
                alert("Failed to load user details.");
            }
        });
    });

    // שליחה של הנתונים המעודכנים
    $('#saveChangesBtn').click(function () {
        var userFormData = $('#userEditsForm').serialize();

        $.ajax({
            url: '/Users/SetEdits', // שימוש בנתיב מוחלט
            type: 'POST',
            data: userFormData,
            success: function (response) {
                if (response.success) {
                    alert(response.message);
                    $('#userEditsModal').modal('hide'); // סגירת המודל אחרי שמירת הנתונים
                    location.reload(); // רענון הדף כדי להציג את הנתונים המעודכנים
                } else {
                    alert("Failed to update details.");
                }
            },
            error: function () {
                alert("An error occurred while saving the details.");
            }
        });
    });
});
$(document).ready(function () {
    $('.userDetailsBtn').click(function () {
        var userId = $(this).data('id'); // קבלת ה-ID מתוך כפתור

        // שליחת בקשה לשרת לקבלת פרטי המשתמש
        $.ajax({
            url: `/Users/Details/${userId}`, // שימוש בנתיב מוחלט
            type: 'GET',
            success: function (data) {
                // הצגת פרטי המשתמש בתוך ה-Modal
                $('#userDetailsContent').html(data);
            },
            error: function () {
                alert("Failed to load user details.");
            }
        });
    });


});

