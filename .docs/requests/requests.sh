user_name = "admin"
user_password = "@admin13"
user_email = "admin@localhost"

api_url = "https://localhost:7110"

curl --request POST \
  --url $api_url/api/v1/auth/register \
  --header 'Content-Type: application/json' \
  --data '{
	"email": "$user_email",
	"password": "$user_password",
	"username": "$user_name",
	"firstName": "Admin",
	"lastName": "Admin"
}'

jwt=$(curl --request POST \
  --url '$api_url/api/v1/auth/login?useCookie=false' \
  --header 'Content-Type: application/json' \
  --data '{
	"UsernameOrEmail": "$user_email",
	"Password": "$user_password"
}')

curl --request POST \
  --url $api_url/api/v1/admin/groups/user/permissions \
  --header 'Content-Type: application/json' \
  --header 'Authorization: Bearer $jwt' \
  --data 'Permissions: ["blog/user", "blog/comment"]'

curl --request POST \
  --url $api_url/api/v1/admin/groups/admin/permissions \
  --header 'Content-Type: application/json' \
  --header 'Authorization: Bearer $jwt' \
  --data 'Permissions: ["blog/moderator", "blog/admin"]'

jwt=$(curl --request POST \
  --url '$api_url/api/v1/auth/login?useCookie=false' \
  --header 'Content-Type: application/json' \
  --data '{
	"UsernameOrEmail": "$user_email",
	"Password": "$user_password"
}')

curl --request POST \
  --url $api_url/api/v1/collections \
  --header 'Content-Type: application/json' \ 
  --header 'Authorization: Bearer $jwt' \
  --data '{
	"Slug": "posts",
	"Name": "Posts",
	"Schema": $(cat post_schema.json),
	"Templates": [
		{
			"Slug": "post-detail",
			"SingleItem": true,
			"Template": "$(cat post_detail_template.html)"
		},
		{
			"Slug": "posts",
			"SingleItem": true,
			"Template": "$(cat post_overview_template.html)"
		}
	],
	"InsertPermission": "blog/user",
	"ModifyPermission": "blog/moderator",
	"DeletePermission": "blog/moderator",
	"ComplexQueryPermission": "blog/admin"
}'

curl --request POST \
  --url $api_url/api/v1/collections \
  --header 'Content-Type: application/json' \ 
  --header 'Authorization: Bearer $jwt' \
  --data '{
	"Slug": "comments",
	"Name": "Comments",
	"Schema": $(cat comment_schema.json),
	"Templates": [
		{
			"Slug": "comments",
			"SingleItem": true,
			"Template": "$(cat comments_template.html)"
		},
		{
			"Slug": "comment",
			"SingleItem": true,
			"Template": "$(cat comment_template.html)"
		}
	],
	"DefaultTemplate": "comment",
	"InsertPermission": "blog/comment",
	"ModifyPermission": "blog/moderator",
	"DeletePermission": "blog/moderator",
	"ComplexQueryPermission": "blog/admin"
}'

curl --request POST \
  --url $api_url/api/v1/files \
  --header 'Content-Type: multipart/form-data' \
  --header 'Authorization: Bearer $jwt' \
  --form 'file=@./blog.html' \
  --form slug=blog.html \
  --form virtualPath=static-content
  
curl --request POST \
  --url $api_url/api/v1/files \
  --header 'Content-Type: multipart/form-data' \
  --header 'Authorization: Bearer $jwt' \
  --form 'file=@./styles.css' \
  --form slug=styles.css \
  --form virtualPath=static-content
  
curl --request POST \
  --url $api_url/api/v1/routes \
  --header 'Content-Type: application/json' \
  --header 'Authorization: Bearer $jwt' \
  --data '{
	"UrlTemplate": "blog/posts/{postSlug}",
	"CollectionSlug": "posts",
	"TemplateSlug": "post-detail",
	"StaticTemplate": "static-content/blog.html",
	"Paginate": false,
	"Fields": [
		{
			"ParameterName": "postSlug",
			"DocumentFieldName": "slug",
			"MatchKind": 0,
			"BsonType": 2,
			"IsNullable": false,
			"IsOptional": false
		}
	]
}'

curl --request POST \
  --url $api_url/api/v1/routes \
  --header 'Content-Type: application/json' \
  --header 'Authorization: Bearer $jwt' \
  --data '{
	"UrlTemplate": "index.html",
	"RedirectUrl": "$api_url/blog/posts",	
}'

curl --request POST \
  --url $api_url/api/v1/routes \
  --header 'Content-Type: application/json' \
  --header 'Authorization: Bearer $jwt' \
  --data '{
	"UrlTemplate": "blog/posts",
	"CollectionSlug": "posts",
	"TemplateSlug": "posts",
	"StaticTemplate": "static-content/blog.html",
	"Paginate": true
}'

curl --request POST \
  --url $api_url/api/v1/routes \
  --header 'Content-Type: application/json' \
  --header 'Authorization: Bearer $jwt' \
  --data '{
	"UrlTemplate": "blog/posts/{postId}/comments",
	"CollectionSlug": "comments",
	"TemplateSlug": "comments",
	"Paginate": true,
	"Fields": [
		{
			"ParameterName": "postId",
			"DocumentFieldName": "postId",
			"MatchKind": 0,
			"BsonType": 7,
			"IsNullable": false,
			"IsOptional": false
		}
	]
}'

curl --request POST \
  --url 'https://localhost:7110/api/v1/collections/users/templates' \
  --header 'Content-Type: multipart/form-data' \
  --header 'Authorization: Bearer $jwt' \
  --form Slug=post_author_template \
  --form SingleItem=true \
  --form 'templateFile=@./post_author_template.html'
  
curl --request POST \
  --url 'https://localhost:7110/api/v1/collections/users/templates' \
  --header 'Content-Type: multipart/form-data' \
  --header 'Authorization: Bearer $jwt' \
  --form Slug=comment_author_template \
  --form SingleItem=true \
  --form 'templateFile=@./comment_author_template.html'

curl --request POST \
  --url $api_url/api/v1/routes \
  --header 'Content-Type: application/json' \
  --header 'Authorization: Bearer $jwt' \
  --data '{
	"UrlTemplate": "users/{userId}/{templateSlug}",
	"CollectionSlug": "users",
	"Paginate": true,
	"Fields": [
		{
			"ParameterName": "userId",
			"DocumentFieldName": "_id",
			"MatchKind": 0,
			"BsonType": 7,
			"IsNullable": false,
			"IsOptional": false
		}
	]
}'

curl --request POST \
  --url $api_url/api/v1/collections/posts \
  --header 'Content-Type: application/json' \
  --header 'Authorization: Bearer $jwt' \
  --data '{
	"title": "My first post",
	"slug": "my-first-post",
	"summary": "My first post on this blog"
	"content": "#My First Post\n\nThis is my first post on my new fancy blog, please lets be polite and enjoy ourselves ^^"
}'